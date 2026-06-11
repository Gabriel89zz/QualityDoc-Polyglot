USE QualityDocDB;
GO

-- =======================================================
-- DATOS SEMILLA (SEED) - QualityDoc Multi-tenant
-- Corre este script DESPUÉS de haber creado las tablas.
-- =======================================================

-- 1. CATÁLOGO DE ROLES
INSERT INTO Roles (role_name) VALUES
('Super Admin'),        -- ID 1: Tú (Dueño del software)
('Admin de Empresa'),   -- ID 2: Cliente principal (Ej. Gerente de Calidad)
('Creador de Doc'),     -- ID 3: Ingeniero que redacta
('Revisor'),            -- ID 4: Gerente que revisa
('Aprobador'),          -- ID 5: Director que aprueba
('Operario'),           -- ID 6: Operador en piso (Solo lectura) - ACTUALIZADO
('Auditor');            -- ID 7: Revisor de trazabilidad - NUEVO
GO

-- 2. CATÁLOGO DE NORMAS
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
('Falcons Manufacturing', 'FALC-123456-789'),   -- ID 1: Empresa de prueba 1
('Merco Supermercados', 'MERC-987654-321');     -- ID 2: Empresa de prueba 2
GO

-- 5. DEPARTAMENTOS
INSERT INTO Departments (company_id, dept_name) VALUES
(1, 'Aseguramiento de Calidad'),   -- ID 1 (Pertenece a Falcons)
(1, 'Ingeniería de Producción'),   -- ID 2 (Pertenece a Falcons)
(2, 'Recursos Humanos');          -- ID 3 (Pertenece a Merco)
GO

-- =======================================================
-- 6. USUARIOS BASE Y EQUIPOS POR DEPARTAMENTO
-- =======================================================
INSERT INTO Users (company_id, dept_id, role_id, full_name, email, password_hash, created_by) VALUES
-- --- ADMINISTRADORES PRINCIPALES ---
-- Tu usuario Super Admin (NULL en company y dept)
(NULL, NULL, 1, 'Hector Torres', 'elcomparosh97@gmail.com', '$2a$12$.uPJW3BoFdrdTjPuMHKXUeNldtKtmDK/ysKzOwcqM7QBNSGpXIeaG', NULL),
-- Admins de cada tenant
(1, 1, 2, 'Admin Falcons', 'yega2632@gmail.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 1),
(2, 3, 2, 'Admin Merco', 'torres.gabriel33zz@gmail.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 1),

-- --- FALCONS: DEPARTAMENTO 1 (Aseguramiento de Calidad) ---
(1, 1, 3, 'Creador Calidad', 'creador.ca@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 1, 4, 'Revisor Calidad', 'revisor.ca@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 1, 5, 'Aprobador Calidad', 'aprobador.ca@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 1, 6, 'Operario Calidad', 'operario.ca@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 1, 7, 'Auditor Calidad', 'auditor.ca@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),

-- --- FALCONS: DEPARTAMENTO 2 (Ingeniería de Producción) ---
(1, 2, 3, 'Creador Produccion', 'creador.pr@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 2, 4, 'Revisor Produccion', 'revisor.pr@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 2, 5, 'Aprobador Produccion', 'aprobador.pr@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 2, 6, 'Operario Produccion', 'operario.pr@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),
(1, 2, 7, 'Auditor Produccion', 'auditor.pr@falcons.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 2),

-- --- MERCO: DEPARTAMENTO 3 (Auditoría Interna) ---
(2, 3, 3, 'Creador Merco', 'creador.me@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 3),
(2, 3, 4, 'Revisor Merco', 'revisor.me@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 3),
(2, 3, 5, 'Aprobador Merco', 'aprobador.me@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 3),
(2, 3, 6, 'Operario Merco', 'operario.me@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 3),
(2, 3, 7, 'Auditor Merco', 'auditor.me@merco.com', '$2a$12$dR9OqQ6S.iJITr8wMQJ/N.9scRHa66h3P7AbpKvOGvf6yNgeIWhFq', 3);
GO

-- =======================================================
-- 7. CATEGORÍAS DOCUMENTALES (Estructura ISO / IATF)
-- =======================================================
INSERT INTO DocumentCategories (company_id, norm_id, category_name, prefix, description, hierarchy_level, retention_years, created_by) VALUES
-- Estructura ISO 9001 para Falcons (Empresa 1, Norma 1)
(1, 1, 'Manual de Calidad', 'ISO-MAN', 'Documento maestro del SGC', 1, 5, 1),
(1, 1, 'Procedimientos', 'ISO-PRO', 'Procedimientos operativos estándar', 2, 3, 1),
(1, 1, 'Instrucciones', 'ISO-INS', 'Instrucciones de trabajo en piso', 3, 3, 1),
(1, 1, 'Formatos', 'ISO-FOR', 'Plantillas en blanco para llenado', 4, 3, 1),
(1, 1, 'Registros', 'ISO-REG', 'Evidencia de actividades completadas', 5, 5, 1),
(1, 1, 'Externos', 'ISO-EXT', 'Documentación de origen externo', 6, 3, 1),

-- Estructura IATF 16949 para Falcons (Empresa 1, Norma 2)
(1, 2, 'Manual de Calidad', 'IATF-MAN', 'Manual del sistema automotriz', 1, 5, 1),
(1, 2, 'Procedimientos', 'IATF-PRO', 'Procedimientos core del sistema', 2, 3, 1),
(1, 2, 'Instrucciones', 'IATF-INS', 'Instrucciones operativas específicas', 3, 3, 1),
(1, 2, 'Registros', 'IATF-REG', 'Registros de calidad con retención estricta', 4, 7, 1),

-- Estructura ISO 9001 para Merco (Empresa 2, Norma 1)
(2, 1, 'Manual de Calidad', 'ISO-MAN', 'Documento maestro del SGC', 1, 5, 1),
(2, 1, 'Procedimientos', 'ISO-PRO', 'Procedimientos operativos estándar', 2, 3, 1),
(2, 1, 'Instrucciones', 'ISO-INS', 'Instrucciones de trabajo en piso', 3, 3, 1),
(2, 1, 'Formatos', 'ISO-FOR', 'Plantillas en blanco para llenado', 4, 3, 1),
(2, 1, 'Registros', 'ISO-REG', 'Evidencia de actividades completadas', 5, 5, 1),
(2, 1, 'Externos', 'ISO-EXT', 'Documentación de origen externo', 6, 3, 1),

-- Estructura IATF 16949 para Merco (Empresa 2, Norma 2)
(2, 2, 'Manual de Calidad', 'IATF-MAN', 'Manual del sistema automotriz', 1, 5, 1),
(2, 2, 'Procedimientos', 'IATF-PRO', 'Procedimientos core del sistema', 2, 3, 1),
(2, 2, 'Instrucciones', 'IATF-INS', 'Instrucciones operativas específicas', 3, 3, 1),
(2, 2, 'Registros', 'IATF-REG', 'Registros de calidad con retención estricta', 4, 7, 1);
GO

