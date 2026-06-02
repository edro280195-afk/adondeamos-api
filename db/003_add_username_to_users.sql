-- db/003_add_username_to_users.sql
-- Fase 6: Agregar Username a los usuarios para el nuevo flujo de login

-- 1. Agregar la columna (temporalmente permitiendo nulos para datos existentes)
ALTER TABLE users ADD COLUMN username VARCHAR(50);

-- 2. Llenar los datos existentes (asignar un username temporal basado en el id o el inicio del email)
UPDATE users SET username = split_part(email, '@', 1) || '_' || left(id::text, 4) WHERE username IS NULL;

-- 3. Hacer la columna obligatoria
ALTER TABLE users ALTER COLUMN username SET NOT NULL;

-- 4. Agregar índice único para asegurar que los usernames no se repitan
CREATE UNIQUE INDEX idx_users_username ON users (lower(username));
