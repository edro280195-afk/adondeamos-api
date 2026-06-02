-- =====================================================================
-- [nombre por definir] — Esquema inicial (Núcleo V1 + ganchos Fase 2)
-- PostgreSQL 13+   ·   Identificadores en inglés, comentarios en español
-- =====================================================================
-- Decisiones de diseño clave:
--  1) El "guardado" pertenece SIEMPRE a un usuario (user_id obligatorio).
--     Un usuario en solitario funciona desde el día uno, sin grupo.
--  2) El "grupo" (pareja, familia, amigos) es una capa OPCIONAL para
--     compartir, no el centro de la app. Solo existe si el usuario lo crea.
--  3) Una lista puede ser personal (group_id NULL) o de grupo (group_id
--     definido). La boveda compartida de la pareja es solo una lista de grupo.
--  4) "places" es la bisagra. google_place_id es lo UNICO que se guarda de
--     Google (indice unico parcial => un mismo lugar nunca se duplica).
--     origin='own' = lugar propio que tu das de alta (tu dato propietario).
--  5) La seccion "FASE 2" (resenas, fotos, follows, insignias) se incluye
--     para no repintar despues, pero se activa cuando haya densidad local.
-- =====================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;  -- habilita gen_random_uuid()

-- ---------------------------------------------------------------------
-- Tipos enumerados
-- ---------------------------------------------------------------------
CREATE TYPE place_origin       AS ENUM ('google', 'own');                 -- origen del lugar
CREATE TYPE social_network     AS ENUM ('tiktok','instagram','facebook','whatsapp','google_maps','youtube','manual'); -- de donde salio el guardado
CREATE TYPE save_status        AS ENUM ('pending', 'visited');            -- pendiente / ya visitado
CREATE TYPE content_visibility AS ENUM ('private', 'group', 'public');    -- privado / compartido con grupos / publico
CREATE TYPE group_role         AS ENUM ('owner', 'member');               -- rol dentro del grupo
CREATE TYPE interaction_type   AS ENUM ('view','save','vote','visit');    -- senal para el recomendador (Fase 3)

-- ---------------------------------------------------------------------
-- Funcion para mantener updated_at automaticamente
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- NUCLEO (V1)
-- =====================================================================

-- Usuarios -------------------------------------------------------------
CREATE TABLE users (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name          text NOT NULL,                         -- nombre visible
  email         text NOT NULL,                         -- correo de acceso
  password_hash text,                                  -- nulo si entra con login social
  avatar_url    text,                                  -- foto de perfil (archivo en R2/S3, aqui solo la URL)
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX uq_users_email ON users (lower(email));  -- correo unico, sin importar mayusculas

-- Grupos (pareja, familia, amigos) — OPCIONAL --------------------------
CREATE TABLE groups (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name       text NOT NULL,                            -- ej. "Eduardo y esposa", "Familia"
  created_by uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE group_members (
  group_id  uuid NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
  user_id   uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  role      group_role NOT NULL DEFAULT 'member',
  joined_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (group_id, user_id)
);
CREATE INDEX ix_group_members_user ON group_members (user_id);  -- listar los grupos de un usuario

-- Lugares (la bisagra) -------------------------------------------------
CREATE TABLE places (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  origin          place_origin NOT NULL,               -- 'google' o 'own'
  google_place_id text,                                -- UNICO campo que se guarda de Google; null si es propio
  name            text,                                -- obligatorio para 'own'; para 'google' es cache opcional
  latitude        numeric(9,6),
  longitude       numeric(9,6),
  city            text,
  created_by      uuid REFERENCES users(id) ON DELETE SET NULL,  -- quien lo dio de alta (relevante en 'own')
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  -- Si viene de Google, debe traer google_place_id:
  CONSTRAINT places_google_needs_place_id CHECK (origin <> 'google' OR google_place_id IS NOT NULL),
  -- Si es propio, debe traer nombre:
  CONSTRAINT places_own_needs_name        CHECK (origin <> 'own'    OR name IS NOT NULL)
);
-- Un mismo lugar de Google nunca se duplica (canoniza por place_id):
CREATE UNIQUE INDEX uq_places_google_place_id ON places (google_place_id) WHERE google_place_id IS NOT NULL;
CREATE INDEX ix_places_city ON places (city);

-- Guardados (pertenecen al usuario) ------------------------------------
CREATE TABLE saves (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id        uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,  -- DUENO del guardado
  place_id       uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  source_network social_network NOT NULL DEFAULT 'manual',  -- red de donde salio
  source_url     text,                                  -- el enlace del post/video que vio
  thumbnail_url  text,                                  -- miniatura (archivo en R2/S3, aqui solo la URL)
  note           text,                                  -- nota personal ("ir un viernes", "pedir los de pastor")
  visibility     content_visibility NOT NULL DEFAULT 'private',
  status         save_status NOT NULL DEFAULT 'pending',
  created_at     timestamptz NOT NULL DEFAULT now(),
  updated_at     timestamptz NOT NULL DEFAULT now(),
  visited_at     timestamptz,                           -- cuando se marco como visitado
  -- Un usuario no guarda dos veces el mismo lugar:
  CONSTRAINT uq_saves_user_place UNIQUE (user_id, place_id)
);
CREATE INDEX ix_saves_user   ON saves (user_id);
CREATE INDEX ix_saves_place  ON saves (place_id);
CREATE INDEX ix_saves_status ON saves (user_id, status);  -- para "pendientes por visitar"

-- Listas (personales o de grupo) ---------------------------------------
CREATE TABLE lists (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,  -- quien creo la lista
  group_id   uuid REFERENCES groups(id) ON DELETE SET NULL,         -- NULL = personal; definido = de grupo (boveda compartida)
  name       text NOT NULL,                                          -- "Quiero ir", "Citas", "Antojos"
  visibility content_visibility NOT NULL DEFAULT 'private',
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_lists_owner ON lists (owner_id);
CREATE INDEX ix_lists_group ON lists (group_id);

-- Items de lista (una lista de grupo agrega guardados de varios duenos) -
CREATE TABLE list_items (
  list_id  uuid NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
  save_id  uuid NOT NULL REFERENCES saves(id) ON DELETE CASCADE,
  added_by uuid REFERENCES users(id) ON DELETE SET NULL,  -- quien lo agrego (relevante en listas de grupo)
  position int NOT NULL DEFAULT 0,                          -- orden manual
  added_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (list_id, save_id)
);
CREATE INDEX ix_list_items_save ON list_items (save_id);

-- Decidir / match ------------------------------------------------------
CREATE TABLE decision_sessions (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  group_id   uuid REFERENCES groups(id) ON DELETE CASCADE,  -- NULL = en solitario (ruleta/"a donde voy"); definido = match en grupo
  created_by uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  context    text,                                          -- contexto usado (clima, fecha, presupuesto)
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_decision_sessions_group ON decision_sessions (group_id);

CREATE TABLE decision_options (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id uuid NOT NULL REFERENCES decision_sessions(id) ON DELETE CASCADE,
  place_id   uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  CONSTRAINT uq_decision_options UNIQUE (session_id, place_id)  -- no repetir un lugar en la misma sesion
);
CREATE INDEX ix_decision_options_session ON decision_options (session_id);

CREATE TABLE votes (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  option_id  uuid NOT NULL REFERENCES decision_options(id) ON DELETE CASCADE,
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  is_yes     boolean NOT NULL,                              -- swipe: true = si, false = no
  created_at timestamptz NOT NULL DEFAULT now(),
  -- Un usuario vota una sola vez por opcion:
  CONSTRAINT uq_votes_option_user UNIQUE (option_id, user_id)
);

CREATE TABLE decision_matches (
  session_id uuid NOT NULL REFERENCES decision_sessions(id) ON DELETE CASCADE,
  place_id   uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  matched_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (session_id, place_id)  -- el lugar donde todos coincidieron (el plan que se hizo)
);

-- =====================================================================
-- FASE 2 — Capa social / el foso local
-- (incluida para no repintar; se construye/activa cuando haya densidad)
-- =====================================================================

-- Resenas de TUS usuarios (no de Google) -------------------------------
CREATE TABLE reviews (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  place_id   uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  body       text,                                          -- opinion en texto
  rating     int  CHECK (rating BETWEEN 1 AND 5),           -- 1 a 5 estrellas
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_reviews_user_place UNIQUE (user_id, place_id)  -- una resena por usuario por lugar
);
CREATE INDEX ix_reviews_place ON reviews (place_id);

-- Fotos de TUS usuarios (archivo en R2/S3, aqui solo la URL) -----------
CREATE TABLE photos (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  place_id   uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  url        text NOT NULL,
  caption    text,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_photos_place ON photos (place_id);

-- Seguidores (grafo social) --------------------------------------------
CREATE TABLE follows (
  follower_id  uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,  -- quien sigue
  following_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,  -- a quien sigue
  created_at   timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (follower_id, following_id),
  CONSTRAINT follows_no_self CHECK (follower_id <> following_id)      -- nadie se sigue a si mismo
);
CREATE INDEX ix_follows_following ON follows (following_id);           -- listar seguidores de alguien

-- Insignias (el motor de estatus) --------------------------------------
CREATE TABLE badges (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  code        text NOT NULL UNIQUE,                          -- identificador estable, ej. "top_recommender_nld"
  name        text NOT NULL,                                 -- "Recomendador #1 de Nuevo Laredo"
  description text,
  criteria    text                                           -- regla para otorgarla (texto/JSON simple)
);

CREATE TABLE user_badges (
  user_id    uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  badge_id   uuid NOT NULL REFERENCES badges(id) ON DELETE CASCADE,
  awarded_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, badge_id)
);

-- Interacciones (senal para el recomendador — Fase 3) ------------------
CREATE TABLE interactions (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    uuid NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  place_id   uuid NOT NULL REFERENCES places(id) ON DELETE CASCADE,
  type       interaction_type NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_interactions_user_place ON interactions (user_id, place_id);

-- =====================================================================
-- Triggers de updated_at
-- =====================================================================
CREATE TRIGGER trg_users_updated   BEFORE UPDATE ON users   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_places_updated  BEFORE UPDATE ON places  FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_saves_updated   BEFORE UPDATE ON saves   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_lists_updated   BEFORE UPDATE ON lists   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_reviews_updated BEFORE UPDATE ON reviews FOR EACH ROW EXECUTE FUNCTION set_updated_at();
