/* =====================================================================
   Sample data for EShopSource.
   Idempotent: clears the tables and reseeds with explicit ids via
   IDENTITY_INSERT, so the referential ids stay deterministic and stable
   no matter how many times the script runs.
   ===================================================================== */

USE EShopSource;
GO

DELETE FROM dbo.ord_ln;
DELETE FROM dbo.ord_hdr;
DELETE FROM dbo.prod_t;
DELETE FROM dbo.cust_t;
GO

-- ── Customers (18) ──
SET IDENTITY_INSERT dbo.cust_t ON;
INSERT INTO dbo.cust_t (cust_id, f_nm, l_nm, eml, created_dt, cntry_cd) VALUES
( 1, 'Noa',    'Levi',     'noa.levi@example.com',      '2024-01-15T09:20:00', 'IL'),
( 2, 'Daniel', 'Cohen',    'daniel.cohen@example.com',  '2024-02-03T14:05:00', 'IL'),
( 3, 'Maya',   'Friedman', 'maya.f@example.com',        '2024-02-27T11:40:00', 'IL'),
( 4, 'Liam',   'Smith',    'liam.smith@example.com',    '2024-03-11T08:15:00', 'US'),
( 5, 'Emma',   'Johnson',  'emma.j@example.com',        '2024-03-29T19:30:00', 'US'),
( 6, 'Oliver', 'Brown',    'oliver.brown@example.com',  '2024-04-14T07:55:00', 'GB'),
( 7, 'Sophie', 'Muller',   'sophie.muller@example.com', '2024-04-30T16:10:00', 'DE'),
( 8, 'Lucas',  'Weber',    'lucas.weber@example.com',   '2024-05-19T10:25:00', 'DE'),
( 9, 'Chloe',  'Dubois',   'chloe.dubois@example.com',  '2024-06-02T13:45:00', 'FR'),
(10, 'Adam',   'Katz',     'adam.katz@example.com',     '2024-06-21T12:00:00', 'IL'),
(11, 'Tamar',  'Shapiro',  'tamar.shapiro@example.com', '2024-07-08T09:05:00', 'IL'),
(12, 'Ethan',  'Wilson',   'ethan.wilson@example.com',  '2024-07-25T18:20:00', 'US'),
(13, 'Ava',    'Taylor',   'ava.taylor@example.com',    '2024-08-13T15:35:00', 'GB'),
(14, 'Yael',   'Mizrahi',  'yael.mizrahi@example.com',  '2024-09-01T08:50:00', 'IL'),
(15, 'Noah',   'Anderson', 'noah.a@example.com',        '2024-09-22T20:15:00', 'US'),
(16, 'Mia',    'Rossi',    'mia.rossi@example.com',     '2024-10-10T11:25:00', 'IT'),
(17, 'Itai',   'Bar',      'itai.bar@example.com',      '2024-11-05T14:40:00', 'IL'),
(18, 'Laura',  'Garcia',   'laura.garcia@example.com',  '2024-12-01T09:10:00', 'ES');
SET IDENTITY_INSERT dbo.cust_t OFF;
GO

-- ── Products (12) ──
SET IDENTITY_INSERT dbo.prod_t ON;
INSERT INTO dbo.prod_t (prod_id, prod_nm, cat_cd, unit_prc, in_stock) VALUES
( 1, 'Wireless Mouse',       'ACC',   29.90, 1),
( 2, 'Mechanical Keyboard',  'ACC',  119.00, 1),
( 3, '27" 4K Monitor',       'DISP', 349.00, 1),
( 4, 'USB-C Hub',            'ACC',   45.50, 1),
( 5, 'Laptop Stand',         'ACC',   39.90, 1),
( 6, 'Noise-Cancel Headset', 'AUD',  199.00, 1),
( 7, 'Webcam 1080p',         'CAM',   69.00, 0),
( 8, 'External SSD 1TB',     'STOR', 109.00, 1),
( 9, 'Ergonomic Chair',      'FURN', 289.00, 1),
(10, 'Desk Lamp LED',        'FURN',  34.00, 1),
(11, 'Bluetooth Speaker',    'AUD',   59.90, 0),
(12, 'Graphics Tablet',      'ACC',  149.00, 1);
SET IDENTITY_INSERT dbo.prod_t OFF;
GO

-- ── Order headers (15) ──
SET IDENTITY_INSERT dbo.ord_hdr ON;
INSERT INTO dbo.ord_hdr (ord_id, cust_id, ord_dt, sts, tot_amt) VALUES
( 1,  1, '2024-03-01T10:00:00', 'S', 148.90),
( 2,  2, '2024-03-15T12:30:00', 'S', 349.00),
( 3,  3, '2024-04-02T09:45:00', 'N', 103.80),
( 4,  4, '2024-04-20T16:20:00', 'C', 199.00),
( 5,  5, '2024-05-05T11:10:00', 'S', 548.00),
( 6,  6, '2024-05-22T14:55:00', 'S', 109.00),
( 7,  7, '2024-06-10T08:30:00', 'N', 289.00),
( 8,  8, '2024-06-28T18:05:00', 'S', 119.40),
( 9,  9, '2024-07-14T13:40:00', 'S', 289.00),
(10, 10, '2024-08-01T10:15:00', 'N', 178.90),
(11, 11, '2024-08-19T15:25:00', 'S',  93.90),
(12, 12, '2024-09-07T09:35:00', 'C', 149.00),
(13, 13, '2024-09-30T17:50:00', 'S', 548.00),
(14, 14, '2024-10-18T12:05:00', 'N', 109.00),
(15,  1, '2024-11-12T11:20:00', 'S', 238.90);
SET IDENTITY_INSERT dbo.ord_hdr OFF;
GO

-- ── Order lines (25) ──
SET IDENTITY_INSERT dbo.ord_ln ON;
INSERT INTO dbo.ord_ln (ln_id, ord_id, prod_id, qty, ln_prc) VALUES
( 1,  1,  1, 1,  29.90), ( 2,  1,  2, 1, 119.00),
( 3,  2,  3, 1, 349.00),
( 4,  3,  5, 1,  39.90), ( 5,  3,  1, 1,  29.90), ( 6,  3, 10, 1,  34.00),
( 7,  4,  6, 1, 199.00),
( 8,  5,  3, 1, 349.00), ( 9,  5,  6, 1, 199.00),
(10,  6,  8, 1, 109.00),
(11,  7,  9, 1, 289.00),
(12,  8,  4, 1,  45.50), (13,  8,  5, 1,  39.90), (14,  8, 10, 1,  34.00),
(15,  9,  9, 1, 289.00),
(16, 10,  2, 1, 119.00), (17, 10, 11, 1,  59.90),
(18, 11, 11, 1,  59.90), (19, 11, 10, 1,  34.00),
(20, 12, 12, 1, 149.00),
(21, 13,  3, 1, 349.00), (22, 13,  6, 1, 199.00),
(23, 14,  8, 1, 109.00),
(24, 15,  6, 1, 199.00), (25, 15,  5, 1,  39.90);
SET IDENTITY_INSERT dbo.ord_ln OFF;
GO

PRINT 'EShopSource sample data seeded.';
GO
