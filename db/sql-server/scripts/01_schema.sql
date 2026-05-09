/*
    DATABASE SCRIPT: QualityDoc Multi-tenant System 
*/

USE [master];
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QualityDocDB')
    CREATE DATABASE QualityDocDB;
GO
USE QualityDocDB;
GO

-----------------------------------------------------------
-- 1. TABLAS DE CATÁLOGO (Configuración del Sistema)
-----------------------------------------------------------

CREATE TABLE Roles (
    role_id INT IDENTITY(1,1) PRIMARY KEY,
    role_name NVARCHAR(50) NOT NULL UNIQUE,
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NULL,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE Norms (
    norm_id INT IDENTITY(1,1) PRIMARY KEY,
    norm_name NVARCHAR(50) NOT NULL UNIQUE, 
    release_year NVARCHAR(4) NULL,
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE DocumentStatus (
    status_id INT IDENTITY(1,1) PRIMARY KEY,
    status_name NVARCHAR(30) NOT NULL UNIQUE, 
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

-----------------------------------------------------------
-- 2. GESTIÓN DE COMPAÑÍAS Y USUARIOS (Multi-tenant)
-----------------------------------------------------------

CREATE TABLE Companies (
    company_id INT IDENTITY(1,1) PRIMARY KEY,
    legal_name NVARCHAR(200) NOT NULL,
    tax_id NVARCHAR(20) NOT NULL UNIQUE,
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE Departments (
    dept_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NOT NULL,
    dept_name NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_Depts_Company FOREIGN KEY (company_id) REFERENCES Companies(company_id),

    -- 🚀 CANDADO: Evitar duplicados de departamentos por empresa
    CONSTRAINT UQ_Company_DeptName UNIQUE (company_id, dept_name),
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NULL,
    dept_id INT,
    role_id INT NOT NULL,
    full_name NVARCHAR(200) NOT NULL,
    email NVARCHAR(150) NOT NULL UNIQUE,
    password_hash NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_Users_Company FOREIGN KEY (company_id) REFERENCES Companies(company_id),
    CONSTRAINT FK_Users_Dept FOREIGN KEY (dept_id) REFERENCES Departments(dept_id),
    CONSTRAINT FK_Users_Role FOREIGN KEY (role_id) REFERENCES Roles(role_id),
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NULL, 
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

-----------------------------------------------------------
-- 3. MÓDULO DOCUMENTAL (Estructura ISO 9001/IATF)
-----------------------------------------------------------

CREATE TABLE DocumentCategories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NOT NULL,
    norm_id INT NULL, 
    category_name NVARCHAR(100) NOT NULL, 
    prefix VARCHAR(5) NOT NULL, 
    description VARCHAR(255) NULL, 
    hierarchy_level INT NOT NULL CHECK (hierarchy_level BETWEEN 1 AND 4), 
    CONSTRAINT FK_Categories_Company FOREIGN KEY (company_id) REFERENCES Companies(company_id),
    CONSTRAINT FK_Categories_Norm FOREIGN KEY (norm_id) REFERENCES Norms(norm_id),
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE Documents (
    doc_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NOT NULL,
    category_id INT NOT NULL,
    dept_id INT NOT NULL, 
    doc_code NVARCHAR(50) NOT NULL, 
    doc_name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX), 
    is_external BIT DEFAULT 0, 
    
    -- Restricciones y Llaves Foráneas
    CONSTRAINT UQ_DocCode_Per_Company UNIQUE(company_id, doc_code),
    CONSTRAINT FK_Docs_Company FOREIGN KEY (company_id) REFERENCES Companies(company_id),
    CONSTRAINT FK_Docs_Category FOREIGN KEY (category_id) REFERENCES DocumentCategories(category_id),
    CONSTRAINT FK_Docs_Department FOREIGN KEY (dept_id) REFERENCES Departments(dept_id), 
    
    -- Campos de Auditoría
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NOT NULL, 
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

CREATE TABLE DocumentVersions (
    version_id INT IDENTITY(1,1) PRIMARY KEY,
    doc_id INT NOT NULL,
    status_id INT NOT NULL, 
    version_num NVARCHAR(10) NOT NULL, 
    file_path NVARCHAR(MAX) NOT NULL,
    extension NVARCHAR(10) NOT NULL,
    change_description NVARCHAR(MAX), 
    approved_at DATETIME2 NULL, 
    obsoleted_at DATETIME2 NULL, 
    CONSTRAINT FK_Versions_Doc FOREIGN KEY (doc_id) REFERENCES Documents(doc_id),
    CONSTRAINT FK_Versions_Status FOREIGN KEY (status_id) REFERENCES DocumentStatus(status_id),
    -- Audit Fields
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NOT NULL, 
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

-----------------------------------------------------------
-- 4. FLUJO DE APROBACIÓN (WORKFLOW UNIVERSAL)
-----------------------------------------------------------

CREATE TABLE DocumentApprovals (
    approval_id INT IDENTITY(1,1) PRIMARY KEY,
    version_id INT NOT NULL,
    approver_id INT NOT NULL,
    
    -- Campos de Flujo Estricto (Sin DEFAULTs para forzar al backend a enviar el dato exacto)
    step_order INT NOT NULL,
    step_type NVARCHAR(30) NOT NULL, 
    
    -- Estado de la decisión y evidencia legal
    approval_status NVARCHAR(20) DEFAULT 'Pending', 
    comments NVARCHAR(MAX), 
    signature_token NVARCHAR(MAX), 
    signed_at DATETIME2 NULL,
    
    -- Relaciones Foráneas
    CONSTRAINT FK_Approvals_Version FOREIGN KEY (version_id) REFERENCES DocumentVersions(version_id),
    CONSTRAINT FK_Approvals_User FOREIGN KEY (approver_id) REFERENCES Users(user_id),
    
    -- Candados de Seguridad (Restricciones explícitas y con nombre)
    CONSTRAINT CHK_StepType CHECK (step_type IN ('Elaboró', 'Revisó', 'Aprobó')),
    CONSTRAINT CHK_ApprovalStatus CHECK (approval_status IN ('Pending', 'Approved', 'Rejected')),
    
    -- Audit Fields 
    status NVARCHAR(20) DEFAULT 'Active',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT,
    updated_at DATETIME2,
    updated_by INT,
    deleted_at DATETIME2,
    deleted_by INT
);

-----------------------------------------------------------
-- 5. RESTRICCIONES DE AUDITORÍA
-----------------------------------------------------------

DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(t.name) + 
               ' ADD CONSTRAINT FK_' + t.name + '_CreatedBy FOREIGN KEY (created_by) REFERENCES Users(user_id);' +
               'ALTER TABLE ' + QUOTENAME(t.name) + 
               ' ADD CONSTRAINT FK_' + t.name + '_UpdatedBy FOREIGN KEY (updated_by) REFERENCES Users(user_id);' +
               'ALTER TABLE ' + QUOTENAME(t.name) + 
               ' ADD CONSTRAINT FK_' + t.name + '_DeletedBy FOREIGN KEY (deleted_by) REFERENCES Users(user_id);'
FROM sys.tables t 
WHERE t.name IN ('Roles', 'Norms', 'DocumentStatus', 'Companies', 'Departments', 'Users', 'DocumentCategories', 'Documents', 'DocumentVersions', 'DocumentApprovals');
EXEC sp_executesql @sql;

-----------------------------------------------------------
-- 6. PERFORMANCE INDEXES
-----------------------------------------------------------

CREATE INDEX IX_Users_Login ON Users(email, status);
CREATE INDEX IX_Docs_Company_Status ON Documents(company_id, status);
CREATE INDEX IX_Versions_Doc_Status ON DocumentVersions(doc_id, status_id, status);
CREATE UNIQUE INDEX UIX_Docs_Code_Company ON Documents(company_id, doc_code) WHERE status <> 'Deleted';
CREATE INDEX IX_Approvals_Pending ON DocumentApprovals(approver_id, approval_status);

-----------------------------------------------------------
-- 7. TRIGGERS DE AUDITORÍA Y CONTROL
-----------------------------------------------------------

GO
IF OBJECT_ID('trg_HandleDocumentObsolescence', 'TR') IS NOT NULL DROP TRIGGER trg_HandleDocumentObsolescence;
IF OBJECT_ID('trg_UpdateDocumentTimestamp', 'TR') IS NOT NULL DROP TRIGGER trg_UpdateDocumentTimestamp;
IF OBJECT_ID('trg_Users_UpdateTimestamp', 'TR') IS NOT NULL DROP TRIGGER trg_Users_UpdateTimestamp;
GO

CREATE TRIGGER trg_HandleDocumentObsolescence
ON DocumentVersions
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Inserted i WHERE i.status_id = 3) 
    BEGIN
        DECLARE @DocID INT;
        DECLARE @NewVersionID INT;
        SELECT @DocID = doc_id, @NewVersionID = version_id FROM Inserted;

        UPDATE DocumentVersions
        SET status_id = 4, 
            obsoleted_at = GETUTCDATE(),
            updated_at = GETUTCDATE(),
            updated_by = (SELECT created_by FROM Inserted)
        WHERE doc_id = @DocID 
          AND version_id <> @NewVersionID 
          AND status_id = 3;
    END
END;
GO

CREATE TRIGGER trg_UpdateDocumentTimestamp
ON Documents
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Documents 
    SET updated_at = GETUTCDATE()
    FROM Inserted i WHERE Documents.doc_id = i.doc_id;
END;
GO

CREATE TRIGGER trg_Users_UpdateTimestamp
ON Users
AFTER UPDATE
AS
BEGIN
    IF (ROWCOUNT_BIG() = 0) RETURN;
    IF NOT UPDATE(updated_at)
    BEGIN
        UPDATE Users 
        SET updated_at = GETUTCDATE()
        FROM Users u
        INNER JOIN Inserted i ON u.user_id = i.user_id;
    END
END;
GO

-----------------------------------------------------------
-- 8. PROCEDIMIENTOS ALMACENADOS
-----------------------------------------------------------

GO
IF OBJECT_ID('sp_SignDocumentWorkflow', 'P') IS NOT NULL DROP PROCEDURE sp_SignDocumentWorkflow;
IF OBJECT_ID('sp_GetCompanyQualityKPIs', 'P') IS NOT NULL DROP PROCEDURE sp_GetCompanyQualityKPIs;
IF OBJECT_ID('sp_DisableCompanyComplete', 'P') IS NOT NULL DROP PROCEDURE sp_DisableCompanyComplete;
IF OBJECT_ID('sp_SoftDeleteDocument', 'P') IS NOT NULL DROP PROCEDURE sp_SoftDeleteDocument;
GO

-- 🚀 SP MODIFICADO PARA PASAR LA ESTAFETA (REVISOR -> APROBADOR)
CREATE PROCEDURE sp_SignDocumentWorkflow
    @ApprovalID INT,       
    @ApproverID INT,       
    @Comments NVARCHAR(MAX),
    @SignatureToken NVARCHAR(MAX),
    @IsApproved BIT        
AS
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @VersionID INT;
        DECLARE @CurrentStepOrder INT;
        DECLARE @DocID INT;
        DECLARE @DeptID INT;
        DECLARE @CompanyID INT;
        
        -- 🛡️ CUMPLE CONSTRAINT: Genera 'Approved' o 'Rejected' exactos para CHK_ApprovalStatus
        DECLARE @StatusString NVARCHAR(20) = CASE WHEN @IsApproved = 1 THEN 'Approved' ELSE 'Rejected' END;

        -- 1. Obtenemos el VersionID y el Paso Actual
        SELECT 
            @VersionID = version_id,
            @CurrentStepOrder = step_order
        FROM DocumentApprovals 
        WHERE approval_id = @ApprovalID;

        -- Obtenemos los datos del documento
        SELECT 
            @DocID = doc_id
        FROM DocumentVersions WHERE version_id = @VersionID;

        SELECT 
            @DeptID = dept_id,
            @CompanyID = company_id
        FROM Documents WHERE doc_id = @DocID;

        -- 2. Actualizamos el registro de firma del usuario actual
        UPDATE DocumentApprovals
        SET approval_status = @StatusString,
            comments = @Comments,
            signature_token = @SignatureToken,
            signed_at = GETUTCDATE(),
            updated_at = GETUTCDATE(),
            updated_by = @ApproverID
        WHERE approval_id = @ApprovalID;

        -- 3. Lógica de Workflow Secuencial
        IF @IsApproved = 0
        BEGIN
            -- Si alguien RECHAZA, el documento se regresa a Borrador
            UPDATE DocumentVersions
            SET status_id = 1, 
                updated_at = GETUTCDATE(),
                updated_by = @ApproverID
            WHERE version_id = @VersionID;
        END
        ELSE
        BEGIN
            -- Si APROBÓ, evaluamos qué paso acaba de terminar
            IF @CurrentStepOrder = 1
            BEGIN
                -- Acaba de firmar el REVISOR (Paso 1). Buscar al APROBADOR (Paso 2)
                DECLARE @AprobadorUserID INT;

                SELECT TOP 1 @AprobadorUserID = u.user_id 
                FROM Users u
                INNER JOIN Roles r ON u.role_id = r.role_id
                WHERE r.role_name = 'Aprobador' 
                  AND u.dept_id = @DeptID 
                  AND u.company_id = @CompanyID
                  AND u.status = 'Active';

                IF @AprobadorUserID IS NOT NULL
                BEGIN
                    -- 🛡️ CUMPLE CONSTRAINT: Inserción explícita sin depender de DEFAULTs
                    INSERT INTO DocumentApprovals 
                        (version_id, step_order, step_type, approver_id, approval_status, status, created_at, created_by)
                    VALUES 
                        (@VersionID, 2, 'Aprobó', @AprobadorUserID, 'Pending', 'Active', GETUTCDATE(), @ApproverID);
                END
                ELSE
                BEGIN
                    THROW 50000, 'Aprobación exitosa, pero no se encontró Aprobador en este departamento.', 1;
                END
            END
            ELSE IF @CurrentStepOrder = 2
            BEGIN
                -- Acaba de firmar el APROBADOR (Paso 2). ¡Flujo terminado!
                UPDATE DocumentVersions
                SET status_id = 3, 
                    approved_at = GETUTCDATE(),
                    updated_at = GETUTCDATE(),
                    updated_by = @ApproverID
                WHERE version_id = @VersionID;
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        
        -- 🛡️ DEFENSA ACTIVA: Intercepción del Error 547 (Violación de Constraint CHECK o FOREIGN KEY)
        IF ERROR_NUMBER() = 547
        BEGIN
            THROW 50001, 'Error de integridad: La operación fue bloqueada porque el estado o tipo de paso enviado no es válido según las reglas estrictas.', 1;
        END
        ELSE
        BEGIN
            THROW;
        END
    END CATCH
END;
GO

CREATE PROCEDURE sp_GetCompanyQualityKPIs
    @CompanyID INT
AS
BEGIN
    SELECT 
        ds.status_name as Estado,
        COUNT(dv.version_id) as TotalDocumentos
    FROM DocumentVersions dv
    JOIN DocumentStatus ds ON dv.status_id = ds.status_id
    JOIN Documents d ON dv.doc_id = d.doc_id
    WHERE d.company_id = @CompanyID
    GROUP BY ds.status_name;
END;
GO

CREATE PROCEDURE sp_DisableCompanyComplete
    @CompanyID INT,
    @AdminUserID INT
AS
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE Companies SET status = 'Inactive', deleted_at = GETUTCDATE(), deleted_by = @AdminUserID WHERE company_id = @CompanyID;
        UPDATE Users SET status = 'Inactive', deleted_at = GETUTCDATE(), deleted_by = @AdminUserID WHERE company_id = @CompanyID;
        UPDATE DocumentCategories SET status = 'Inactive', deleted_at = GETUTCDATE(), deleted_by = @AdminUserID WHERE company_id = @CompanyID; 
        UPDATE Documents SET status = 'Inactive', deleted_at = GETUTCDATE(), deleted_by = @AdminUserID WHERE company_id = @CompanyID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_EnableCompanyComplete
    @CompanyID INT,
    @AdminUserID INT
AS
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
        -- 1. Reactivamos la Empresa
        UPDATE Companies 
        SET status = 'Active', deleted_at = NULL, deleted_by = NULL, updated_at = GETUTCDATE(), updated_by = @AdminUserID 
        WHERE company_id = @CompanyID;

        -- 2. Reactivamos a los Usuarios
        UPDATE Users 
        SET status = 'Active', deleted_at = NULL, deleted_by = NULL, updated_at = GETUTCDATE(), updated_by = @AdminUserID 
        WHERE company_id = @CompanyID;

        -- 3. Reactivamos las Categorías
        UPDATE DocumentCategories 
        SET status = 'Active', deleted_at = NULL, deleted_by = NULL, updated_at = GETUTCDATE(), updated_by = @AdminUserID 
        WHERE company_id = @CompanyID; 

        -- 4. Reactivamos los Documentos
        UPDATE Documents 
        SET status = 'Active', deleted_at = NULL, deleted_by = NULL, updated_at = GETUTCDATE(), updated_by = @AdminUserID 
        WHERE company_id = @CompanyID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_SoftDeleteDocument
    @DocID INT,
    @UserID INT,
    @CompanyID INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE Documents 
        SET status = 'Deleted', deleted_at = GETUTCDATE(), deleted_by = @UserID 
        WHERE doc_id = @DocID AND company_id = @CompanyID;

        UPDATE DocumentVersions
        SET status = 'Deleted', deleted_at = GETUTCDATE(), deleted_by = @UserID
        WHERE doc_id = @DocID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO