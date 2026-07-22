/* =====================================================================
   Source database: EShopSource  (the "operational" DB to be described)
   ---------------------------------------------------------------------
   Column names are intentionally terse/technical (cust_t, f_nm, eml, ...)
   to demonstrate the value of the semantic layer: translating cryptic
   physical names into business-friendly ones.

   Idempotent: safe to re-run. Creates the DB if missing and recreates
   the tables from scratch.
   ===================================================================== */

IF DB_ID('EShopSource') IS NULL
    CREATE DATABASE EShopSource;
GO

USE EShopSource;
GO

-- Drop in dependency order so the script is re-runnable.
IF OBJECT_ID('dbo.ord_ln',  'U') IS NOT NULL DROP TABLE dbo.ord_ln;
IF OBJECT_ID('dbo.ord_hdr', 'U') IS NOT NULL DROP TABLE dbo.ord_hdr;
IF OBJECT_ID('dbo.prod_t',  'U') IS NOT NULL DROP TABLE dbo.prod_t;
IF OBJECT_ID('dbo.cust_t',  'U') IS NOT NULL DROP TABLE dbo.cust_t;
GO

-- Customers
CREATE TABLE dbo.cust_t (
    cust_id     INT IDENTITY(1,1) CONSTRAINT PK_cust_t PRIMARY KEY,
    f_nm        NVARCHAR(50)  NOT NULL,
    l_nm        NVARCHAR(50)  NOT NULL,
    eml         NVARCHAR(255) NOT NULL,   -- PII
    created_dt  DATETIME2     NOT NULL CONSTRAINT DF_cust_created DEFAULT SYSUTCDATETIME(),
    cntry_cd    CHAR(2)       NULL        -- internal ISO country code (hidden from business users)
);

-- Products
CREATE TABLE dbo.prod_t (
    prod_id     INT IDENTITY(1,1) CONSTRAINT PK_prod_t PRIMARY KEY,
    prod_nm     NVARCHAR(150) NOT NULL,
    cat_cd      NVARCHAR(50)  NULL,
    unit_prc    DECIMAL(10,2) NOT NULL,
    in_stock    BIT           NOT NULL CONSTRAINT DF_prod_instock DEFAULT 1
);

-- Order headers
CREATE TABLE dbo.ord_hdr (
    ord_id      INT IDENTITY(1,1) CONSTRAINT PK_ord_hdr PRIMARY KEY,
    cust_id     INT           NOT NULL,
    ord_dt      DATETIME2     NOT NULL,
    sts         CHAR(1)       NOT NULL,   -- N = New, S = Shipped, C = Cancelled
    tot_amt     DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_ord_hdr_cust FOREIGN KEY (cust_id) REFERENCES dbo.cust_t(cust_id)
);

-- Order lines
CREATE TABLE dbo.ord_ln (
    ln_id       INT IDENTITY(1,1) CONSTRAINT PK_ord_ln PRIMARY KEY,
    ord_id      INT           NOT NULL,
    prod_id     INT           NOT NULL,
    qty         INT           NOT NULL,
    ln_prc      DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_ord_ln_ord  FOREIGN KEY (ord_id)  REFERENCES dbo.ord_hdr(ord_id),
    CONSTRAINT FK_ord_ln_prod FOREIGN KEY (prod_id) REFERENCES dbo.prod_t(prod_id)
);
GO

PRINT 'EShopSource schema created.';
GO
