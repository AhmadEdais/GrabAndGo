-- ==========================================
-- GRAB & GO: CLEAN & COMPLETE DATABASE SCRIPT
-- ==========================================

USE master;
GO

-- 1. Create the Database fresh
IF DB_ID('GrabAndGoDB') IS NOT NULL
BEGIN
    ALTER DATABASE GrabAndGoDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE GrabAndGoDB;
END
GO

CREATE DATABASE GrabAndGoDB;
GO

-- CRITICAL: Tell SSMS to use the new DB, not 'master' or 'dbo'
USE GrabAndGoDB;
GO

-- ==========================================
-- Phase 0: Lookup Tables (Dictionary Data)
-- ==========================================

CREATE TABLE SessionStatuses (
    SessionStatusId INT PRIMARY KEY,
    StatusName NVARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE PaymentStatuses (
    PaymentStatusId INT PRIMARY KEY,
    StatusName NVARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE LedgerEntryTypes (
    LedgerEntryTypeId INT PRIMARY KEY,
    TypeName NVARCHAR(50) UNIQUE NOT NULL
);

-- Seed Lookup Tables
INSERT INTO SessionStatuses (SessionStatusId, StatusName) VALUES (1, 'Active'), (2, 'Ended');
INSERT INTO PaymentStatuses (PaymentStatusId, StatusName) VALUES (1, 'Pending'), (2, 'Completed'), (3, 'Failed');
INSERT INTO LedgerEntryTypes (LedgerEntryTypeId, TypeName) VALUES (1, 'TopUp'), (2, 'Debit');

-- ==========================================
-- Phase 1: Master Data
-- ==========================================

CREATE TABLE Stores (
    StoreId INT IDENTITY(1,1) PRIMARY KEY,
    StoreCode NVARCHAR(50) UNIQUE NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Timezone NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Zones (
    ZoneId INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(StoreId),
    ZoneCode NVARCHAR(50) NOT NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    ZoneType NVARCHAR(50) NOT NULL,
    Range_X1 DECIMAL(10,3) NOT NULL,
    Range_X2 DECIMAL(10,3) NOT NULL,
    Range_Y1 DECIMAL(10,3) NOT NULL,
    Range_Y2 DECIMAL(10,3) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Cameras (
    CameraId INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(StoreId),
    CameraCode NVARCHAR(50) NOT NULL,
    IpOrStreamUrl NVARCHAR(500) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    SKU NVARCHAR(100) UNIQUE NOT NULL,
    PriceGross DECIMAL(10,2) NOT NULL,
    VAT_Rate DECIMAL(5,4) NOT NULL DEFAULT 0.1600, 
    ImageUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE ProductAiLabels (
    ProductAiLabelId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    AiLabel NVARCHAR(120) NOT NULL,
    ModelVersion NVARCHAR(50) NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 1
);

CREATE TABLE ProductZoneMapping (
    ProductZoneMappingId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    ZoneId INT NOT NULL FOREIGN KEY REFERENCES Zones(ZoneId),
    Priority INT NOT NULL DEFAULT 1
);

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(320) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Wallets (
    WalletId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(UserId),
    CurrentBalance DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Currency CHAR(3) NOT NULL DEFAULT 'JOD',
    LastUpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ==========================================
-- Phase 2: Session Management
-- ==========================================

CREATE TABLE EntryQrTokens (
    EntryQrTokenId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(StoreId),
    TokenHash VARBINARY(32) NOT NULL,
    IssuedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    ConsumedAt DATETIME2 NULL
);

CREATE TABLE Sessions (
    SessionId INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(StoreId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    EntryQrTokenId INT NOT NULL FOREIGN KEY REFERENCES EntryQrTokens(EntryQrTokenId),
    StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndedAt DATETIME2 NULL,
    SessionStatusId INT NOT NULL FOREIGN KEY REFERENCES SessionStatuses(SessionStatusId) DEFAULT 1,
    ExitDetectedAt DATETIME2 NULL
);

CREATE TABLE SessionTrackBindings (
    BindingId INT IDENTITY(1,1) PRIMARY KEY,
    SessionId INT NOT NULL FOREIGN KEY REFERENCES Sessions(SessionId),
    TrackId NVARCHAR(50) NOT NULL,
    Source NVARCHAR(50) NOT NULL,
    BoundAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UnboundAt DATETIME2 NULL,
    IsCurrent BIT NOT NULL DEFAULT 1
);

CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    SessionId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Sessions(SessionId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CartVersion INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ==========================================
-- Phase 3: Vision Events
-- ==========================================

CREATE TABLE VisionEventsRaw (
    VisionEventId INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(StoreId),
    CameraId INT NULL FOREIGN KEY REFERENCES Cameras(CameraId),
    ZoneId INT NULL FOREIGN KEY REFERENCES Zones(ZoneId),
    MatchedSessionId INT NULL FOREIGN KEY REFERENCES Sessions(SessionId),
    TrackId NVARCHAR(50) NOT NULL,
    AiLabel NVARCHAR(120) NOT NULL,
    Action NVARCHAR(10) NOT NULL, 
    EventTime DATETIME2 NOT NULL,
    Confidence DECIMAL(5,4) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    IngestedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ProcessingStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending' 
);

CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL FOREIGN KEY REFERENCES Carts(CartId),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    LastEventId INT NULL FOREIGN KEY REFERENCES VisionEventsRaw(VisionEventId),
    Quantity INT NOT NULL,
    LastAction NVARCHAR(10) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE CartItemEvent (
    CartItemEventId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL FOREIGN KEY REFERENCES Carts(CartId),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    VisionEventId INT NOT NULL FOREIGN KEY REFERENCES VisionEventsRaw(VisionEventId),
    Action NVARCHAR(10) NOT NULL,
    DeltaQty INT NOT NULL,
    CartVersionAfter INT NOT NULL,
    AppliedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ==========================================
-- Phase 4: Financials
-- ==========================================

CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    SessionId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Sessions(SessionId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CartId INT NOT NULL FOREIGN KEY REFERENCES Carts(CartId),
    Subtotal DECIMAL(12,2) NOT NULL,
    Tax DECIMAL(12,2) NOT NULL,
    Total DECIMAL(12,2) NOT NULL,
    PaymentStatusId INT NOT NULL FOREIGN KEY REFERENCES PaymentStatuses(PaymentStatusId) DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,
    FailureReason NVARCHAR(250) NULL
);

CREATE TABLE TransactionItems (
    TransactionItemId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionId INT NOT NULL FOREIGN KEY REFERENCES Transactions(TransactionId),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    UnitPrice DECIMAL(12,2) NOT NULL,
    Quantity INT NOT NULL,
    LineTotal DECIMAL(12,2) NOT NULL
);

CREATE TABLE WalletLedgerEntries (
    LedgerEntryId INT IDENTITY(1,1) PRIMARY KEY,
    WalletId INT NOT NULL FOREIGN KEY REFERENCES Wallets(WalletId),
    RelatedTransactionId INT NULL FOREIGN KEY REFERENCES Transactions(TransactionId),
    LedgerEntryTypeId INT NOT NULL FOREIGN KEY REFERENCES LedgerEntryTypes(LedgerEntryTypeId),
    Amount DECIMAL(12,2) NOT NULL,
    BalanceAfter DECIMAL(12,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Reference NVARCHAR(100) UNIQUE NOT NULL
);

CREATE TABLE Invoices (
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Transactions(TransactionId),
    PdfUrlOrPath NVARCHAR(500) NOT NULL,
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ==========================================
-- MOCK SEED DATA (PHASE 0.6)
-- ==========================================

-- 1. Setup Store
INSERT INTO Stores (StoreCode, Name, Timezone) VALUES ('SWFI', 'Grab&Go - Sweifieh Branch', 'Asia/Amman');
DECLARE @StoreId INT = SCOPE_IDENTITY();

-- 2. Setup Zones
INSERT INTO Zones (StoreId, ZoneCode, DisplayName, ZoneType, Range_X1, Range_X2, Range_Y1, Range_Y2)
VALUES 
(@StoreId, 'ENTRANCE_ZONE', 'Entrance', 'Entrance', 0.000, 1.600, 0.000, 2.200),
(@StoreId, 'SHELF_A', 'Shelf A', 'Shelf', 2.000, 3.400, 1.300, 2.000),
(@StoreId, 'EXIT_ZONE', 'Exit', 'Exit', 4.700, 6.200, 0.000, 2.200);

-- 3. Setup Cameras
INSERT INTO Cameras (StoreId, CameraCode, IpOrStreamUrl)
VALUES 
(@StoreId, 'CAM_01_ENTRANCE', 'rtsp://10.10.1.10/stream1'),
(@StoreId, 'CAM_02_SHELF_A', 'rtsp://10.10.1.11/stream1'),
(@StoreId, 'CAM_03_EXIT', 'rtsp://10.10.1.12/stream1');

-- 4. Setup Products
INSERT INTO Products (Name, SKU, PriceGross, VAT_Rate, ImageUrl)
VALUES 
('Kewpie Mayonnaise 500g', 'KEWPIE-500G', 6.50, 0.1600, 'https://cdn.grabngo.app/img/products/kewpie_500g.png'),
('Coca-Cola Can 330ml', 'COKE-330', 1.20, 0.1600, 'https://cdn.grabngo.app/img/products/coke_330.png');

-- 5. Setup AI Labels
INSERT INTO ProductAiLabels (ProductId, AiLabel, ModelVersion)
VALUES 
((SELECT ProductId FROM Products WHERE SKU = 'KEWPIE-500G'), 'Kewpie_Mayonnaise', 'yolo_v10'),
((SELECT ProductId FROM Products WHERE SKU = 'COKE-330'), 'coca_cola_can', 'yolo_v10');

-- 6. Map Products to Zones
INSERT INTO ProductZoneMapping (ProductId, ZoneId)
VALUES 
((SELECT ProductId FROM Products WHERE SKU = 'KEWPIE-500G'), (SELECT ZoneId FROM Zones WHERE ZoneCode = 'SHELF_A')),
((SELECT ProductId FROM Products WHERE SKU = 'COKE-330'), (SELECT ZoneId FROM Zones WHERE ZoneCode = 'SHELF_A'));

-- 7. Setup Test User (Ahmad)
INSERT INTO Users (FirstName, LastName, Email, PasswordHash)
VALUES ('Ahmad', 'Edais', 'ahmad.edais@grabngo.com', 'HASH_STRING');
DECLARE @TestUserId INT = SCOPE_IDENTITY();

-- 8. Setup Wallet & Top-up
INSERT INTO Wallets (UserId, CurrentBalance, Currency) 
VALUES (@TestUserId, 30.00, 'JOD');
DECLARE @TestWalletId INT = SCOPE_IDENTITY();

INSERT INTO WalletLedgerEntries (WalletId, LedgerEntryTypeId, Amount, BalanceAfter, Reference)
VALUES (@TestWalletId, 1, 30.00, 30.00, 'TOPUP_INITIAL_001');

PRINT 'GrabAndGoDB created and seeded successfully.';

