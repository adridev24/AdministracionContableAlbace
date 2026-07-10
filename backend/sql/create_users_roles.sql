-- Script para PostgreSQL: crear tablas de Usuarios, Roles y UserRoles
-- Asegúrate de que PostgreSQL esté corriendo en localhost:5432 con credenciales postgres/postgres

CREATE DATABASE presupuestos;

\c presupuestos

CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    full_name VARCHAR(200),
    email VARCHAR(200)
);

CREATE TABLE user_roles (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

-- Insertar roles de ejemplo
INSERT INTO roles (name) VALUES ('Admin'), ('User');

-- Inserta un usuario de ejemplo. Reemplaza password_hash por el hash bcrypt real.
-- Para generar el hash puedes usar: BCrypt.Net.BCrypt.HashPassword("YourPassword") en C#
INSERT INTO users (username, password_hash, full_name, email)
VALUES ('admin', '$2b$12$wI5WxGW7ZPxsddjZY6yIeOM1aTz63OW0zof0tRwb0gj.A.kw8bJi2', 'Administrador', 'admin@example.com');

-- Asignar rol Admin al usuario creado
INSERT INTO user_roles (user_id, role_id)
VALUES (
    (SELECT id FROM users WHERE username = 'admin'),
    (SELECT id FROM roles WHERE name = 'Admin')
);

-- Nota: Reemplaza el hash por uno generado y guarda la contraseña en forma segura.
