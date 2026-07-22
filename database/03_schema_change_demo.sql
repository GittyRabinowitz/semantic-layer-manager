/* =====================================================================
   OPTIONAL — schema drift demo.
   Run this AFTER the system has synced once, to demonstrate that a newly
   added column is detected on the next sync and surfaces as "Unmapped"
   (a new, un-curated field is hidden from consumers until mapped).
   Re-runnable.
   ===================================================================== */

USE EShopSource;
GO

IF COL_LENGTH('dbo.cust_t', 'phone') IS NULL
BEGIN
    ALTER TABLE dbo.cust_t ADD phone NVARCHAR(30) NULL;  -- PII: should be masked once mapped
    PRINT 'Added column dbo.cust_t.phone';
END
ELSE
    PRINT 'Column dbo.cust_t.phone already exists.';
GO

-- Give a few customers a phone number so the new column has sample data.
UPDATE dbo.cust_t SET phone = '+972-52-1234567' WHERE cust_id = 1;
UPDATE dbo.cust_t SET phone = '+1-415-555-0142'  WHERE cust_id = 4;
UPDATE dbo.cust_t SET phone = '+44-20-7946-0958' WHERE cust_id = 6;
GO
