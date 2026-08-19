const JSON_HEADERS = { "Content-Type": "application/json; charset=utf-8" };
const MAX_BODY_BYTES = 16 * 1024;
const MAX_PUBLIC_COMMENTS = 60;

export default {
  async fetch(request, env) {
    const origin = request.headers.get("Origin") || "";
    const allowedOrigins = getAllowedOrigins(env);

    if (request.method === "OPTIONS") {
      if (origin && !allowedOrigins.has(origin)) {
        return json({ error: "Origin is not allowed." }, 403, request, env);
      }
      return corsResponse(new Response(null, { status: 204 }), request, env);
    }

    if (origin && !allowedOrigins.has(origin)) {
      return json({ error: "Origin is not allowed." }, 403, request, env);
    }

    try {
      const url = new URL(request.url);

      if (request.method === "GET" && url.pathname === "/api/health") {
        return json({ ok: true, service: "kofge-clicker-comments" }, 200, request, env);
      }

      if (request.method === "GET" && url.pathname === "/api/comments") {
        return await getPublicComments(request, env, url);
      }

      if (request.method === "POST" && url.pathname === "/api/comments") {
        return await createComment(request, env);
      }

      if (url.pathname === "/api/admin/comments" && request.method === "GET") {
        return await getAdminComments(request, env, url);
      }

      const adminCommentMatch = url.pathname.match(/^\/api\/admin\/comments\/([a-zA-Z0-9-]+)$/);
      if (adminCommentMatch && request.method === "PATCH") {
        return await updateComment(request, env, adminCommentMatch[1]);
      }

      if (adminCommentMatch && request.method === "DELETE") {
        return await deleteComment(request, env, adminCommentMatch[1]);
      }

      return json({ error: "Not found." }, 404, request, env);
    } catch (error) {
      console.error("Unhandled comments worker error", error);
      return json({ error: "The comments service is temporarily unavailable." }, 500, request, env);
    }
  }
};

async function getPublicComments(request, env, url) {
  const language = normalizeLanguage(url.searchParams.get("language"));
  const result = await env.DB.prepare(`
    SELECT id, name, message, app_version, language, author_reply, created_at, replied_at
    FROM comments
    WHERE status = 'approved' AND language = ?1
    ORDER BY datetime(approved_at) DESC, datetime(created_at) DESC
    LIMIT ?2
  `).bind(language, MAX_PUBLIC_COMMENTS).all();

  const comments = (result.results || []).map((row) => ({
    id: row.id,
    name: row.name,
    message: row.message,
    appVersion: row.app_version,
    language: row.language,
    authorReply: row.author_reply,
    createdAt: row.created_at,
    repliedAt: row.replied_at
  }));

  return json({ comments }, 200, request, env, { "Cache-Control": "public, max-age=30" });
}

async function createComment(request, env) {
  const contentLength = Number(request.headers.get("Content-Length") || 0);
  if (contentLength > MAX_BODY_BYTES) {
    return json({ error: "Message is too large." }, 413, request, env);
  }

  const payload = await readJson(request);
  const language = normalizeLanguage(payload.language);
  if (String(payload.website || "").trim()) {
    return json({ ok: true, status: "pending" }, 202, request, env);
  }

  const name = normalizeInlineText(payload.name);
  const message = normalizeMessage(payload.message);
  const appVersion = normalizeInlineText(payload.appVersion);

  const validationError = validateComment(name, message, appVersion, language);
  if (validationError) {
    return json({ error: localizeError(language, validationError) }, 400, request, env);
  }

  if (!payload.turnstileToken) {
    return json({ error: localizeError(language, "turnstile") }, 400, request, env);
  }

  const ipAddress = request.headers.get("CF-Connecting-IP") || "unknown";
  const turnstileValid = await verifyTurnstile(payload.turnstileToken, ipAddress, env);
  if (!turnstileValid) {
    return json({ error: localizeError(language, "turnstile") }, 400, request, env);
  }

  const fingerprint = await createFingerprint(ipAddress, env.FINGERPRINT_SECRET);
  const recent = await env.DB.prepare(`
    SELECT COUNT(*) AS count
    FROM comments
    WHERE fingerprint = ?1 AND datetime(created_at) >= datetime('now', '-45 seconds')
  `).bind(fingerprint).first();

  if (Number(recent?.count || 0) > 0) {
    return json({ error: localizeError(language, "rate") }, 429, request, env);
  }

  const duplicate = await env.DB.prepare(`
    SELECT id
    FROM comments
    WHERE fingerprint = ?1 AND message = ?2 AND datetime(created_at) >= datetime('now', '-1 day')
    LIMIT 1
  `).bind(fingerprint, message).first();

  if (duplicate) {
    return json({ error: localizeError(language, "duplicate") }, 409, request, env);
  }

  const now = new Date().toISOString();
  const id = crypto.randomUUID();
  await env.DB.prepare(`
    INSERT INTO comments (
      id, name, message, app_version, language, status, fingerprint,
      author_reply, created_at, updated_at, approved_at, replied_at
    ) VALUES (?1, ?2, ?3, ?4, ?5, 'pending', ?6, '', ?7, ?7, NULL, NULL)
  `).bind(id, name, message, appVersion, language, fingerprint, now).run();

  return json({ ok: true, status: "pending" }, 202, request, env);
}

async function getAdminComments(request, env, url) {
  if (!await isAdmin(request, env)) {
    return json({ error: "Unauthorized." }, 401, request, env);
  }

  const requestedStatus = String(url.searchParams.get("status") || "pending").toLowerCase();
  const status = ["pending", "approved", "rejected", "all"].includes(requestedStatus)
    ? requestedStatus
    : "pending";

  const statement = status === "all"
    ? env.DB.prepare(`
        SELECT id, name, message, app_version, language, status, author_reply,
               created_at, updated_at, approved_at, replied_at
        FROM comments
        ORDER BY datetime(created_at) DESC
        LIMIT 100
      `)
    : env.DB.prepare(`
        SELECT id, name, message, app_version, language, status, author_reply,
               created_at, updated_at, approved_at, replied_at
        FROM comments
        WHERE status = ?1
        ORDER BY datetime(created_at) DESC
        LIMIT 100
      `).bind(status);

  const result = await statement.all();
  return json({ comments: result.results || [] }, 200, request, env, { "Cache-Control": "no-store" });
}

async function updateComment(request, env, id) {
  if (!await isAdmin(request, env)) {
    return json({ error: "Unauthorized." }, 401, request, env);
  }

  const payload = await readJson(request);
  const existing = await env.DB.prepare("SELECT id, status FROM comments WHERE id = ?1").bind(id).first();
  if (!existing) {
    return json({ error: "Comment not found." }, 404, request, env);
  }

  const status = payload.status === undefined ? existing.status : String(payload.status).toLowerCase();
  if (!["pending", "approved", "rejected"].includes(status)) {
    return json({ error: "Invalid moderation status." }, 400, request, env);
  }

  const authorReply = payload.authorReply === undefined
    ? null
    : normalizeMessage(payload.authorReply);
  if (authorReply !== null && authorReply.length > 1200) {
    return json({ error: "Developer reply must not exceed 1200 characters." }, 400, request, env);
  }

  const now = new Date().toISOString();
  await env.DB.prepare(`
    UPDATE comments
    SET status = ?2,
        author_reply = CASE WHEN ?3 IS NULL THEN author_reply ELSE ?3 END,
        updated_at = ?4,
        approved_at = CASE
          WHEN ?2 = 'approved' AND approved_at IS NULL THEN ?4
          WHEN ?2 != 'approved' THEN NULL
          ELSE approved_at
        END,
        replied_at = CASE
          WHEN ?3 IS NULL THEN replied_at
          WHEN ?3 = '' THEN NULL
          ELSE ?4
        END
    WHERE id = ?1
  `).bind(id, status, authorReply, now).run();

  return json({ ok: true }, 200, request, env, { "Cache-Control": "no-store" });
}

async function deleteComment(request, env, id) {
  if (!await isAdmin(request, env)) {
    return json({ error: "Unauthorized." }, 401, request, env);
  }

  const result = await env.DB.prepare("DELETE FROM comments WHERE id = ?1").bind(id).run();
  if (!result.meta?.changes) {
    return json({ error: "Comment not found." }, 404, request, env);
  }

  return json({ ok: true }, 200, request, env, { "Cache-Control": "no-store" });
}

async function verifyTurnstile(token, remoteIp, env) {
  if (!env.TURNSTILE_SECRET) {
    throw new Error("TURNSTILE_SECRET is not configured");
  }

  const body = new FormData();
  body.set("secret", env.TURNSTILE_SECRET);
  body.set("response", String(token));
  body.set("remoteip", remoteIp);

  const response = await fetch("https://challenges.cloudflare.com/turnstile/v0/siteverify", {
    method: "POST",
    body
  });
  if (!response.ok) {
    return false;
  }

  const result = await response.json();
  return result.success === true;
}

async function createFingerprint(ipAddress, secret) {
  if (!secret) {
    throw new Error("FINGERPRINT_SECRET is not configured");
  }

  const bytes = new TextEncoder().encode(`${secret}:${ipAddress}`);
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

async function isAdmin(request, env) {
  if (!env.ADMIN_TOKEN) {
    return false;
  }

  const authorization = request.headers.get("Authorization") || "";
  const provided = authorization.startsWith("Bearer ") ? authorization.slice(7) : "";
  if (!provided) {
    return false;
  }

  const encoder = new TextEncoder();
  const [providedHash, expectedHash] = await Promise.all([
    crypto.subtle.digest("SHA-256", encoder.encode(provided)),
    crypto.subtle.digest("SHA-256", encoder.encode(env.ADMIN_TOKEN))
  ]);
  const a = new Uint8Array(providedHash);
  const b = new Uint8Array(expectedHash);
  let difference = a.length ^ b.length;
  for (let index = 0; index < Math.min(a.length, b.length); index += 1) {
    difference |= a[index] ^ b[index];
  }
  return difference === 0;
}

function validateComment(name, message, appVersion) {
  if (name.length < 2 || name.length > 40) return "name";
  if (message.length < 5 || message.length > 1200) return "message";
  if (appVersion.length > 30) return "version";
  if (containsUnsafeProtocol(name) || containsUnsafeProtocol(message)) return "link";
  return "";
}

function containsUnsafeProtocol(value) {
  return /(?:javascript|data|vbscript):/i.test(value);
}

function normalizeInlineText(value) {
  return String(value || "").replace(/[\u0000-\u001F\u007F]+/g, " ").replace(/\s+/g, " ").trim();
}

function normalizeMessage(value) {
  return String(value || "")
    .replace(/\r\n?/g, "\n")
    .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "")
    .replace(/\n{4,}/g, "\n\n\n")
    .trim();
}

function normalizeLanguage(value) {
  return String(value || "").toLowerCase() === "ru" ? "ru" : "en";
}

function localizeError(language, key) {
  const messages = language === "ru"
    ? {
        name: "Имя должно содержать от 2 до 40 символов.",
        message: "Отзыв должен содержать от 5 до 1200 символов.",
        version: "Название версии слишком длинное.",
        link: "Сообщение содержит небезопасную ссылку.",
        turnstile: "Не удалось пройти проверку защиты от спама.",
        rate: "Подождите немного перед отправкой следующего отзыва.",
        duplicate: "Такой отзыв уже был отправлен."
      }
    : {
        name: "The display name must contain 2 to 40 characters.",
        message: "The review must contain 5 to 1200 characters.",
        version: "The version name is too long.",
        link: "The message contains an unsafe link.",
        turnstile: "The anti-spam check could not be completed.",
        rate: "Please wait before submitting another review.",
        duplicate: "This review has already been submitted."
      };
  return messages[key] || messages.message;
}

async function readJson(request) {
  const text = await request.text();
  if (new TextEncoder().encode(text).byteLength > MAX_BODY_BYTES) {
    throw new Error("Request body is too large");
  }
  try {
    return JSON.parse(text || "{}");
  } catch {
    return {};
  }
}

function getAllowedOrigins(env) {
  return new Set(String(env.ALLOWED_ORIGINS || "https://kofge1.github.io")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean));
}

function corsResponse(response, request, env) {
  const headers = new Headers(response.headers);
  const origin = request.headers.get("Origin") || "";
  if (origin && getAllowedOrigins(env).has(origin)) {
    headers.set("Access-Control-Allow-Origin", origin);
    headers.set("Vary", "Origin");
  }
  headers.set("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS");
  headers.set("Access-Control-Allow-Headers", "Authorization, Content-Type");
  headers.set("Access-Control-Max-Age", "86400");
  headers.set("X-Content-Type-Options", "nosniff");
  headers.set("Referrer-Policy", "strict-origin-when-cross-origin");
  return new Response(response.body, { status: response.status, statusText: response.statusText, headers });
}

function json(payload, status, request, env, extraHeaders = {}) {
  const response = new Response(JSON.stringify(payload), {
    status,
    headers: { ...JSON_HEADERS, ...extraHeaders }
  });
  return corsResponse(response, request, env);
}
