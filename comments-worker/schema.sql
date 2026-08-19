CREATE TABLE IF NOT EXISTS comments (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  message TEXT NOT NULL,
  app_version TEXT NOT NULL DEFAULT '',
  language TEXT NOT NULL DEFAULT 'en' CHECK (language IN ('en', 'ru')),
  status TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'rejected')),
  fingerprint TEXT NOT NULL,
  author_reply TEXT NOT NULL DEFAULT '',
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  approved_at TEXT,
  replied_at TEXT
);

CREATE INDEX IF NOT EXISTS idx_comments_status_created
  ON comments(status, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_comments_language_status_created
  ON comments(language, status, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_comments_fingerprint_created
  ON comments(fingerprint, created_at DESC);
