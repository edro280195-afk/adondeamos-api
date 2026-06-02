-- =====================================================================
-- 002 — Invitaciones a grupos (confirmación del invitado)
-- Se aplica DESPUÉS de db/001_init_schema.sql, sobre la misma base de Neon.
-- Identificadores en inglés, comentarios en español.
-- Motivo: agregar a alguien a un grupo ya no es directo; ahora se invita y
-- el invitado debe ACEPTAR para entrar a group_members.
-- =====================================================================

-- Estado de una invitación
CREATE TYPE invitation_status AS ENUM ('pending', 'accepted', 'rejected');

-- Invitaciones para unirse a un grupo
CREATE TABLE group_invitations (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  group_id     uuid NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
  invited_user uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,  -- a quién se invita
  invited_by   uuid REFERENCES users(id) ON DELETE SET NULL,           -- quién invitó
  status       invitation_status NOT NULL DEFAULT 'pending',
  created_at   timestamptz NOT NULL DEFAULT now(),
  responded_at timestamptz                                             -- cuándo se aceptó/rechazó
);
CREATE INDEX ix_group_invitations_invited_user ON group_invitations (invited_user);  -- "mis invitaciones"
CREATE INDEX ix_group_invitations_group        ON group_invitations (group_id);

-- Una sola invitación PENDIENTE por (grupo, usuario):
CREATE UNIQUE INDEX uq_group_invitations_pending
  ON group_invitations (group_id, invited_user) WHERE status = 'pending';
