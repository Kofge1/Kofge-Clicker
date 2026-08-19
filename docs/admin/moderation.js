(() => {
  const config = window.KOFGE_COMMENTS_CONFIG || {};
  const apiBaseUrl = String(config.apiBaseUrl || "").replace(/\/$/, "");
  const loginSection = document.querySelector("[data-admin-login]");
  const dashboard = document.querySelector("[data-admin-dashboard]");
  const loginForm = document.querySelector("[data-admin-login-form]");
  const loginStatus = document.querySelector("[data-admin-login-status]");
  const status = document.querySelector("[data-admin-status]");
  const list = document.querySelector("[data-admin-list]");
  const filter = document.querySelector("[data-admin-filter]");
  const count = document.querySelector("[data-admin-count]");
  const refresh = document.querySelector("[data-admin-refresh]");
  const logout = document.querySelector("[data-admin-logout]");
  const tokenKey = "kofge-comments-admin-token";
  let token = sessionStorage.getItem(tokenKey) || "";

  function setMessage(element, message, kind = "") {
    element.textContent = message;
    element.dataset.kind = kind;
  }

  async function api(path, options = {}) {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      ...options,
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...(options.headers || {})
      }
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      const error = new Error(payload.error || `Request failed (${response.status})`);
      error.status = response.status;
      throw error;
    }
    return payload;
  }

  function formatDate(value) {
    if (!value) return "";
    return new Intl.DateTimeFormat("en-GB", {
      dateStyle: "medium",
      timeStyle: "short"
    }).format(new Date(value));
  }

  function button(text, className, action) {
    const element = document.createElement("button");
    element.type = "button";
    element.className = `btn ${className}`;
    element.textContent = text;
    element.addEventListener("click", action);
    return element;
  }

  function renderComments(comments) {
    list.innerHTML = "";
    count.textContent = `${comments.length} review${comments.length === 1 ? "" : "s"}`;
    if (!comments.length) {
      const empty = document.createElement("div");
      empty.className = "panel moderation-empty";
      empty.textContent = "No reviews in this category.";
      list.append(empty);
      return;
    }

    for (const comment of comments) {
      const card = document.createElement("article");
      card.className = "panel moderation-card";

      const header = document.createElement("div");
      header.className = "moderation-card-head";
      const title = document.createElement("div");
      const name = document.createElement("h2");
      name.textContent = comment.name;
      const badge = document.createElement("span");
      badge.className = "moderation-status";
      badge.textContent = comment.status;
      title.append(name, badge);

      const meta = document.createElement("div");
      meta.className = "moderation-meta";
      const details = document.createElement("span");
      details.textContent = `${String(comment.language).toUpperCase()}${comment.app_version ? ` · ${comment.app_version}` : ""}`;
      const date = document.createElement("span");
      date.textContent = formatDate(comment.created_at);
      meta.append(details, date);
      header.append(title, meta);

      const body = document.createElement("p");
      body.className = "moderation-body";
      body.textContent = comment.message;

      const replyWrap = document.createElement("div");
      replyWrap.className = "moderation-reply";
      const replyLabel = document.createElement("label");
      replyLabel.textContent = "Developer reply";
      const reply = document.createElement("textarea");
      reply.maxLength = 1200;
      reply.value = comment.author_reply || "";
      reply.placeholder = "Optional public reply from Kofge…";
      replyWrap.append(replyLabel, reply);

      const actions = document.createElement("div");
      actions.className = "moderation-actions";
      actions.append(
        button("Approve", "moderation-approve", () => moderate(comment.id, "approved", reply.value)),
        button("Reject", "moderation-reject", () => moderate(comment.id, "rejected", reply.value)),
        button("Save reply", "btn-secondary", () => moderate(comment.id, comment.status, reply.value)),
        button("Delete", "moderation-delete", () => remove(comment.id))
      );

      card.append(header, body, replyWrap, actions);
      list.append(card);
    }
  }

  async function loadComments(showLoginError = false) {
    try {
      const payload = await api(`/api/admin/comments?status=${encodeURIComponent(filter.value)}`);
      loginSection.hidden = true;
      dashboard.hidden = false;
      setMessage(status, "");
      renderComments(Array.isArray(payload.comments) ? payload.comments : []);
      return true;
    } catch (error) {
      if (error.status === 401) {
        token = "";
        sessionStorage.removeItem(tokenKey);
        dashboard.hidden = true;
        loginSection.hidden = false;
        if (showLoginError) setMessage(loginStatus, "The administrator token is incorrect.", "error");
      } else {
        setMessage(status, error.message, "error");
        if (showLoginError) setMessage(loginStatus, error.message, "error");
      }
      return false;
    }
  }

  async function moderate(id, nextStatus, authorReply) {
    setMessage(status, "Saving…");
    try {
      await api(`/api/admin/comments/${encodeURIComponent(id)}`, {
        method: "PATCH",
        body: JSON.stringify({ status: nextStatus, authorReply })
      });
      setMessage(status, "Changes saved.", "success");
      await loadComments();
    } catch (error) {
      setMessage(status, error.message, "error");
    }
  }

  async function remove(id) {
    if (!window.confirm("Delete this review permanently?")) return;
    setMessage(status, "Deleting…");
    try {
      await api(`/api/admin/comments/${encodeURIComponent(id)}`, { method: "DELETE" });
      setMessage(status, "Review deleted.", "success");
      await loadComments();
    } catch (error) {
      setMessage(status, error.message, "error");
    }
  }

  loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!apiBaseUrl) {
      setMessage(loginStatus, "Set apiBaseUrl in comments-config.js before using moderation.", "error");
      return;
    }
    token = String(new FormData(loginForm).get("token") || "");
    sessionStorage.setItem(tokenKey, token);
    setMessage(loginStatus, "Checking…");
    await loadComments(true);
  });

  refresh.addEventListener("click", () => loadComments());
  filter.addEventListener("change", () => loadComments());
  logout.addEventListener("click", () => {
    token = "";
    sessionStorage.removeItem(tokenKey);
    dashboard.hidden = true;
    loginSection.hidden = false;
    loginForm.reset();
    setMessage(loginStatus, "Moderation locked.");
  });

  if (token && apiBaseUrl) {
    loadComments();
  }
})();
