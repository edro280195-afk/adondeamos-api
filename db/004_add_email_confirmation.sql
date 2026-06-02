-- =====================================================================
-- 004 — Confirmación de correo electrónico
-- Se aplica después de db/001..003 sobre la misma base de Neon.
-- Escrito de forma IDEMPOTENTE: seguro de correr aunque alguna pieza
-- ya exista (ADD COLUMN IF NOT EXISTS, CREATE INDEX IF NOT EXISTS, etc.)
-- Identificadores en inglés, comentarios en español.
-- =====================================================================

-- ─────────────────────────────────────────────────────────────────────
-- 1. Agregar columna email_confirmed a users (si no existe)
--    email ya existe desde db/001 con NOT NULL y su índice único.
-- ─────────────────────────────────────────────────────────────────────
ALTER TABLE users
  ADD COLUMN IF NOT EXISTS email_confirmed boolean NOT NULL DEFAULT false;

-- ─────────────────────────────────────────────────────────────────────
-- 2. Tabla de tokens de verificación de email (one-time, hashed)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS email_verification_tokens (
  id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id      uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash   text        NOT NULL,         -- SHA-256 del token en claro (nunca se guarda el token real)
  expires_at   timestamptz NOT NULL,
  consumed_at  timestamptz,                  -- nulo = disponible; con valor = ya usado
  created_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_email_verification_tokens_user
  ON email_verification_tokens (user_id);

CREATE INDEX IF NOT EXISTS ix_email_verification_tokens_hash
  ON email_verification_tokens (token_hash);

-- ─────────────────────────────────────────────────────────────────────
-- 3. Tabla de control de migraciones (registro de scripts aplicados)
--    Permite saber sin duda qué scripts ya corrieron en cada base.
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS schema_migrations (
  filename    text        PRIMARY KEY,
  applied_at  timestamptz NOT NULL DEFAULT now()
);

-- Registra los scripts anteriores (idempotente: INSERT … ON CONFLICT DO NOTHING)
INSERT INTO schema_migrations (filename) VALUES
  ('001_init_schema.sql'),
  ('002_group_invitations.sql'),
  ('003_add_username_to_users.sql'),
  ('004_add_email_confirmation.sql')
ON CONFLICT (filename) DO NOTHING;
