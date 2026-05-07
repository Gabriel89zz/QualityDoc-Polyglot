USE QualityDocDB;
GO

-- =======================================================
-- DATOS SEMILLA (SEED) - QualityDoc Multi-tenant
-- Corre este script DESPUÉS de haber creado las tablas.
-- =======================================================

-- 1. CATÁLOGO DE ROLES
-- Definimos los niveles de acceso del sistema
INSERT INTO Roles (role_name) VALUES
('Super Admin'),        -- ID 1: Tú (Dueño del software)
('Admin de Empresa'),   -- ID 2: Cliente principal (Ej. Gerente de Calidad)
('Creador de Doc'),     -- ID 3: Ingeniero que redacta
('Revisor'),            -- ID 4: Gerente que revisa
('Aprobador'),          -- ID 5: Director que aprueba
('Lector');             -- ID 6: Operador en piso (Solo lectura)
GO

-- 2. CATÁLOGO DE NORMAS (Ahora con año de release)
INSERT INTO Norms (norm_name, release_year) VALUES
('ISO 9001:2015', '2015'),
('IATF 16949:2016', '2016'),
('ISO 14001:2015', '2015'),
('ISO 27001:2022', '2022'),
('ISO 45001:2018', '2018');
GO

-- 3. CATÁLOGO DE ESTADOS DEL DOCUMENTO (Para el Workflow)
INSERT INTO DocumentStatus (status_name) VALUES
('Borrador'),       -- ID 1: Cuando apenas se está redactando
('En Revisión'),    -- ID 2: Circulando por las firmas
('Aprobado'),       -- ID 3: Documento vigente oficial
('Obsoleto');       -- ID 4: Reemplazado por una nueva versión
GO

-- 4. EMPRESAS (Multi-tenant)
INSERT INTO Companies (legal_name, tax_id) VALUES
('QualityDoc System Root', 'ROOT-000000-000'),  -- ID 1: Tu entorno maestro
('Falcons Manufacturing', 'FALC-123456-789'),   -- ID 2: Empresa de prueba 1
('Merco Supermercados', 'MERC-987654-321');     -- ID 3: Empresa de prueba 2
GO

-- 5. DEPARTAMENTOS
INSERT INTO Departments (company_id, dept_name) VALUES
(1, 'Administración de Software'), -- ID 1 (Pertenece a QualityDoc)
(2, 'Aseguramiento de Calidad'),   -- ID 2 (Pertenece a Falcons)
(2, 'Ingeniería de Producción'),   -- ID 3 (Pertenece a Falcons)
(3, 'Auditoría Interna');          -- ID 4 (Pertenece a Merco)
GO

-- 6. USUARIOS BASE PARA PRUEBAS
-- NOTA: El 'password_hash' debe coincidir con el encriptador que uses en C# (Bcrypt, Identity, etc.)
-- Aquí pongo un string temporal genérico para que no truene la BD.
INSERT INTO Users (company_id, dept_id, role_id, full_name, email, password_hash, created_by) VALUES
-- Tu usuario Super Admin (NULL en company_id y dept_id porque es el dueño supremo del sistema)
(1, 1, 1, 'Hector Torres', 'hector@qualitydoc.com', '$2a$12$.uPJW3BoFdrdTjPuMHKXUeNldtKtmDK/ysKzOwcqM7QBNSGpXIeaG', NULL),

-- Usuario administrador del cliente Falcons (Ellos sí tienen empresa y departamento asignado)
(2, 2, 2, 'Admin Falcons', 'calidad@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 1),

-- Usuario administrador del cliente Merco (Ellos sí tienen empresa y departamento asignado)
(3, 4, 2, 'Auditor Merco', 'auditor@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 1);
GO