# Kofge-Clicker comments service

This Cloudflare Worker stores anonymous website reviews in D1. New reviews are
created with the `pending` status and appear publicly only after moderation.

## Privacy and abuse protection

- No email address or account is required.
- Raw IP addresses are not stored.
- A salted SHA-256 fingerprint is used only for rate limiting.
- Cloudflare Turnstile validates public submissions.
- The public API returns approved reviews only.
- Moderation endpoints require a secret administrator token.

## Initial deployment

Requirements: Node.js 20+ and a Cloudflare account.

```powershell
cd comments-worker
npm install
npx wrangler login
npx wrangler d1 create kofge-clicker-comments
```

Copy `wrangler.toml.example` to `wrangler.toml`, then replace
`REPLACE_WITH_D1_DATABASE_ID` with the database ID printed by Wrangler.

Create the database schema:

```powershell
npm run db:remote
```

Create three secrets. Use separate long random values for `ADMIN_TOKEN` and
`FINGERPRINT_SECRET`.

```powershell
npx wrangler secret put TURNSTILE_SECRET
npx wrangler secret put ADMIN_TOKEN
npx wrangler secret put FINGERPRINT_SECRET
```

`TURNSTILE_SECRET` comes from a Turnstile widget created for
`kofge1.github.io` in the Cloudflare dashboard.

Deploy the Worker:

```powershell
npm run deploy
```

Wrangler prints a URL similar to:

```text
https://kofge-clicker-comments.<account-subdomain>.workers.dev
```

Add that URL and the public Turnstile site key to
`docs/comments-config.js`:

```js
window.KOFGE_COMMENTS_CONFIG = Object.freeze({
  apiBaseUrl: "https://kofge-clicker-comments.<account-subdomain>.workers.dev",
  turnstileSiteKey: "YOUR_PUBLIC_TURNSTILE_SITE_KEY"
});
```

After GitHub Pages publishes the updated `docs` directory, reviews are
available at:

- `https://kofge1.github.io/Kofge-Clicker/#reviews`
- `https://kofge1.github.io/Kofge-Clicker/ru/#reviews`

The private moderation page is:

- `https://kofge1.github.io/Kofge-Clicker/admin/comments.html`

The administrator token is kept in `sessionStorage`, so it is removed when the
browser tab is closed. Never commit this token to Git.

## Local development

Copy the example configuration and create `.dev.vars`:

```text
TURNSTILE_SECRET=<Turnstile test or production secret>
ADMIN_TOKEN=<local administrator token>
FINGERPRINT_SECRET=<local random secret>
```

Then initialize the local database and start Wrangler:

```powershell
npm run db:local
npm run dev
```
