SET NOCOUNT ON;
GO

/* =========================================================
   Store
   ========================================================= */

SET IDENTITY_INSERT dbo.Stores ON;

MERGE dbo.Stores AS target
USING
(
    VALUES
        (1, N'SWFI', N'Grab&Go - Sweifieh Branch', N'Asia/Amman', CAST(1 AS bit))
) AS source
(
    StoreId,
    StoreCode,
    Name,
    Timezone,
    IsActive
)
ON target.StoreId = source.StoreId

WHEN MATCHED THEN
    UPDATE SET
        StoreCode = source.StoreCode,
        Name = source.Name,
        Timezone = source.Timezone,
        IsActive = source.IsActive

WHEN NOT MATCHED THEN
    INSERT
    (
        StoreId,
        StoreCode,
        Name,
        Timezone,
        IsActive
    )
    VALUES
    (
        source.StoreId,
        source.StoreCode,
        source.Name,
        source.Timezone,
        source.IsActive
    );

SET IDENTITY_INSERT dbo.Stores OFF;
GO

/* =========================================================
   Zones
   ========================================================= */

SET IDENTITY_INSERT dbo.Zones ON;

MERGE dbo.Zones AS target
USING
(
    VALUES
        (1, 1, N'ENTRANCE_ZONE', N'Entrance', N'Entrance', 0.000, 1.600, 0.000, 2.200),
        (2, 1, N'SHELF_A', N'Shelf A', N'Shelf', 2.000, 3.400, 1.300, 2.000),
        (3, 1, N'EXIT_ZONE', N'Exit', N'Exit', 4.700, 6.200, 0.000, 2.200)
) AS source
(
    ZoneId,
    StoreId,
    ZoneCode,
    DisplayName,
    ZoneType,
    Range_X1,
    Range_X2,
    Range_Y1,
    Range_Y2
)
ON target.ZoneId = source.ZoneId

WHEN MATCHED THEN
    UPDATE SET
        StoreId = source.StoreId,
        ZoneCode = source.ZoneCode,
        DisplayName = source.DisplayName,
        ZoneType = source.ZoneType,
        Range_X1 = source.Range_X1,
        Range_X2 = source.Range_X2,
        Range_Y1 = source.Range_Y1,
        Range_Y2 = source.Range_Y2

WHEN NOT MATCHED THEN
    INSERT
    (
        ZoneId,
        StoreId,
        ZoneCode,
        DisplayName,
        ZoneType,
        Range_X1,
        Range_X2,
        Range_Y1,
        Range_Y2
    )
    VALUES
    (
        source.ZoneId,
        source.StoreId,
        source.ZoneCode,
        source.DisplayName,
        source.ZoneType,
        source.Range_X1,
        source.Range_X2,
        source.Range_Y1,
        source.Range_Y2
    );

SET IDENTITY_INSERT dbo.Zones OFF;
GO

/* =========================================================
   Cameras
   ========================================================= */

SET IDENTITY_INSERT dbo.Cameras ON;

MERGE dbo.Cameras AS target
USING
(
    VALUES
        (1, 1, N'CAM_01_ENTRANCE', N'rtsp://10.10.1.10/stream1', CAST(1 AS bit), 1),
        (2, 1, N'CAM_02_SHELF_A', N'rtsp://10.10.1.11/stream1', CAST(1 AS bit), 2),
        (3, 1, N'CAM_03_EXIT', N'rtsp://10.10.1.12/stream1', CAST(1 AS bit), 3),
        (5, 1, N'CAM_DEMO', N'rtsp://10.10.1.13/stream1', CAST(1 AS bit), 1)
) AS source
(
    CameraId,
    StoreId,
    CameraCode,
    IpOrStreamUrl,
    IsActive,
    ZoneId
)
ON target.CameraId = source.CameraId

WHEN MATCHED THEN
    UPDATE SET
        StoreId = source.StoreId,
        CameraCode = source.CameraCode,
        IpOrStreamUrl = source.IpOrStreamUrl,
        IsActive = source.IsActive,
        ZoneId = source.ZoneId

WHEN NOT MATCHED THEN
    INSERT
    (
        CameraId,
        StoreId,
        CameraCode,
        IpOrStreamUrl,
        IsActive,
        ZoneId
    )
    VALUES
    (
        source.CameraId,
        source.StoreId,
        source.CameraCode,
        source.IpOrStreamUrl,
        source.IsActive,
        source.ZoneId
    );

SET IDENTITY_INSERT dbo.Cameras OFF;
GO

/* =========================================================
   Active products only
   ========================================================= */

SET IDENTITY_INSERT dbo.Products ON;

MERGE dbo.Products AS target
USING
(
    VALUES
        (1,  N'Kewpie Mayonnaise 500g',              N'KEWPIE-500G',         6.50, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (13, N'Al Rabie Mango Juice 250ml',           N'ALRABIE-MANGO-250',   0.35, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (14, N'Teeba Mineral Water 500ml',            N'TEEBA-WATER-500',     0.35, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (15, N'Nabulsi Cheese 400g',                  N'NABULSI-CHEESE-400',  3.50, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (16, N'Nabil Halawa 400g',                    N'NABIL-HALAWA-400',    2.25, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (17, N'Rani Orange Juice 250ml',              N'RANI-ORANGE-250',     0.50, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (18, N'Indomie Mi Goreng Noodles 80g',        N'INDOMIE-MIGORENG-80', 0.45, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (19, N'Chipsy Potato Chips 40g',              N'CHIPSY-CHIPS-40',     0.35, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (20, N'Lipton Yellow Label Tea 100 Bags',     N'LIPTON-TEA-100',      2.75, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (21, N'Kraft Cheese Triangles 8pc 120g',      N'KRAFT-CHEESE-120',    1.50, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit)),
        (22, N'Al Durra Freekeh 500g',                N'DURRA-FREEKEH-500',   2.00, 0.1600, CAST(NULL AS nvarchar(500)), CAST(1 AS bit))
) AS source
(
    ProductId,
    Name,
    SKU,
    PriceGross,
    VAT_Rate,
    ImageUrl,
    IsActive
)
ON target.ProductId = source.ProductId

WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        SKU = source.SKU,
        PriceGross = source.PriceGross,
        VAT_Rate = source.VAT_Rate,
        ImageUrl = source.ImageUrl,
        IsActive = source.IsActive

WHEN NOT MATCHED THEN
    INSERT
    (
        ProductId,
        Name,
        SKU,
        PriceGross,
        VAT_Rate,
        ImageUrl,
        IsActive
    )
    VALUES
    (
        source.ProductId,
        source.Name,
        source.SKU,
        source.PriceGross,
        source.VAT_Rate,
        source.ImageUrl,
        source.IsActive
    );

SET IDENTITY_INSERT dbo.Products OFF;
GO

/* =========================================================
   AI labels for active products
   ========================================================= */

SET IDENTITY_INSERT dbo.ProductAiLabels ON;

MERGE dbo.ProductAiLabels AS target
USING
(
    VALUES
        (1,  1,  N'Kewpie_Mayonnaise',        N'yolo_v10', CAST(1 AS bit)),
        (13, 13, N'al_rabie_mango_juice',     N'yolo_v10', CAST(1 AS bit)),
        (14, 14, N'teeba_water',              N'yolo_v10', CAST(1 AS bit)),
        (15, 15, N'nabulsi_cheese',           N'yolo_v10', CAST(1 AS bit)),
        (16, 16, N'nabil_halawa',             N'yolo_v10', CAST(1 AS bit)),
        (17, 17, N'rani_orange_juice',        N'yolo_v10', CAST(1 AS bit)),
        (18, 18, N'indomie_migoreng',          N'yolo_v10', CAST(1 AS bit)),
        (19, 19, N'chipsy_chips',              N'yolo_v10', CAST(1 AS bit)),
        (20, 20, N'lipton_tea',                N'yolo_v10', CAST(1 AS bit)),
        (21, 21, N'kraft_cheese_triangles',    N'yolo_v10', CAST(1 AS bit)),
        (22, 22, N'durra_freekeh',             N'yolo_v10', CAST(1 AS bit))
) AS source
(
    ProductAiLabelId,
    ProductId,
    AiLabel,
    ModelVersion,
    IsPrimary
)
ON target.ProductAiLabelId = source.ProductAiLabelId

WHEN MATCHED THEN
    UPDATE SET
        ProductId = source.ProductId,
        AiLabel = source.AiLabel,
        ModelVersion = source.ModelVersion,
        IsPrimary = source.IsPrimary

WHEN NOT MATCHED THEN
    INSERT
    (
        ProductAiLabelId,
        ProductId,
        AiLabel,
        ModelVersion,
        IsPrimary
    )
    VALUES
    (
        source.ProductAiLabelId,
        source.ProductId,
        source.AiLabel,
        source.ModelVersion,
        source.IsPrimary
    );

SET IDENTITY_INSERT dbo.ProductAiLabels OFF;
GO

/* =========================================================
   Product-zone mappings
   ========================================================= */

SET IDENTITY_INSERT dbo.ProductZoneMapping ON;

MERGE dbo.ProductZoneMapping AS target
USING
(
    VALUES
        (1, 1, 2, 1)
) AS source
(
    ProductZoneMappingId,
    ProductId,
    ZoneId,
    Priority
)
ON target.ProductZoneMappingId = source.ProductZoneMappingId

WHEN MATCHED THEN
    UPDATE SET
        ProductId = source.ProductId,
        ZoneId = source.ZoneId,
        Priority = source.Priority

WHEN NOT MATCHED THEN
    INSERT
    (
        ProductZoneMappingId,
        ProductId,
        ZoneId,
        Priority
    )
    VALUES
    (
        source.ProductZoneMappingId,
        source.ProductId,
        source.ZoneId,
        source.Priority
    );

SET IDENTITY_INSERT dbo.ProductZoneMapping OFF;
GO