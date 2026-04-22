USE [GrabAndGoDB]
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_InsertUser]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Variables to hold the shredded JSON data
    DECLARE @FirstName NVARCHAR(50), 
            @LastName NVARCHAR(50), 
            @Email NVARCHAR(320), 
            @PasswordHash NVARCHAR(256);

    -- 1. Shred the incoming JSON payload into SQL variables
    SELECT 
        @FirstName = FirstName, 
        @LastName = LastName, 
        @Email = Email, 
        @PasswordHash = PasswordHash
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        FirstName NVARCHAR(50) '$.FirstName',
        LastName NVARCHAR(50) '$.LastName',
        Email NVARCHAR(320) '$.Email',
        PasswordHash NVARCHAR(256) '$.PasswordHash'
    );

    -- 2. Check if email exists (Business Rule)
    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        -- Return -1 in the JSON response to signal a duplicate without throwing an exception
        SET @P_JSON_RESPONSE = '{"NewUserId": -1}'; 
        RETURN;
    END

    DECLARE @NewUserId INT;

    -- 3. Begin Transaction Block
    BEGIN TRY
        BEGIN TRANSACTION;

            -- A. Insert the User
            INSERT INTO [dbo].[Users] ([FirstName], [LastName], [Email], [PasswordHash])
            VALUES (@FirstName, @LastName, @Email, @PasswordHash);

            -- B. Capture the new UserId immediately
            SET @NewUserId = SCOPE_IDENTITY();

            -- C. Create a Wallet for the new User
            INSERT INTO [dbo].[Wallets] ([UserId])
            VALUES (@NewUserId);

        -- Commit both inserts
        COMMIT TRANSACTION;

        -- 4. Format the success response to match a C# Response class/DTO
        -- We construct the JSON directly from the scalar variable
        SET @P_JSON_RESPONSE = (
            SELECT @NewUserId AS NewUserId 
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        -- Throw the system exception so the C# executor can catch and log it
        ;THROW;
    END CATCH
END
GO
--------------------------------------------------------------------------------------------------
USE [GrabAndGoDB]
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_GetUserByEmail_JSON]
    @Email NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;

    -- Look up the active user by email and return their profile + the hash
    SELECT 
        UserId,
        FirstName,
        LastName,
        Email,
        PasswordHash
    FROM [dbo].[Users]
    WHERE 
        Email = @Email 
        AND IsActive = 1
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    -- Note: If no user is found, SQL Server returns NULL, 
    -- which SqlExecutor will neatly deserialize into a null C# object.
END
--------------------------------------------------------------------------------------------------

GO
CREATE OR ALTER PROCEDURE [dbo].[sp_GetUserById_JSON]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- We select the exact property names to match AuthResponseDto
    -- Token is explicitly selected as NULL so it exists in the JSON payload
    SELECT 
        UserId,
        FirstName,
        LastName,
        Email,
        NULL AS Token
    FROM [dbo].[Users]
    WHERE 
        UserId = @UserId 
        AND IsActive = 1 -- Best practice: ensure we only return active users
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;
END
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GenerateEntryQrToken]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Declare variables (Note the string for the incoming hash)
    DECLARE @UserId INT, 
            @StoreId INT, 
            @TokenHashString VARCHAR(64); 
            
    -- 2. Extract from JSON
    SELECT 
        @UserId = UserId, 
        @StoreId = StoreId, 
        @TokenHashString = TokenHash 
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        UserId INT '$.UserId',
        StoreId INT '$.StoreId',
        TokenHash VARCHAR(64) '$.TokenHash'      
    );

    -- 3. Convert Hex String back to VARBINARY(32)
    -- The '2' tells SQL Server to expect a Hex string without the "0x" prefix
    DECLARE @TokenHash VARBINARY(32) = CONVERT(VARBINARY(32), @TokenHashString, 2);

    DECLARE @EntryQrTokenId INT;
    
    -- 4. Calculate exact UTC timestamps
    DECLARE @IssuedAt DATETIME2 = GETUTCDATE();
    DECLARE @ExpiresAt DATETIME2 = DATEADD(MINUTE, 30, @IssuedAt); -- FIX: Corrected to ExpiresAt

    BEGIN TRY
         -- 5. Insert
         INSERT INTO EntryQrTokens (UserId, StoreId, TokenHash, IssuedAt, ExpiresAt)
         VALUES (@UserId, @StoreId, @TokenHash, @IssuedAt, @ExpiresAt);   
            
         SET @EntryQrTokenId = SCOPE_IDENTITY(); 
         
         -- 6. Return Data
         SET @P_JSON_RESPONSE = (
            SELECT 
                @EntryQrTokenId AS TokenId,
                @ExpiresAt AS ExpiresAt -- FIX: Corrected to ExpiresAt
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
         );
    END TRY 
    BEGIN CATCH
        ;THROW;
    END CATCH
END
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetTokenForVerification]
    @TokenId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        EntryQrTokenId AS TokenId,
        UserId,
        StoreId,
        -- Convert VARBINARY(32) back to a 64-character Hex string for JSON transport
        CONVERT(VARCHAR(64), TokenHash, 2) AS TokenHash,
        IssuedAt,
        ExpiresAt,
        ConsumedAt
    FROM EntryQrTokens
    WHERE EntryQrTokenId = @TokenId
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;
END
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_ProcessStoreEntry]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Declare variables
    DECLARE @TokenId INT, 
            @UserId INT, 
            @StoreId INT;

    -- 2. Extract from JSON
    SELECT 
        @TokenId = TokenId,
        @UserId = UserId,
        @StoreId = StoreId
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        TokenId INT '$.TokenId',
        UserId INT '$.UserId',
        StoreId INT '$.StoreId'
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 3. Double-Entry Prevention
        -- We check if the user is ALREADY in a session at ANY store
        IF EXISTS (SELECT 1 FROM Sessions WHERE UserId = @UserId AND SessionStatusId = 1)
        BEGIN
            ;THROW 50001, 'User already has an active shopping session.', 1;
        END
        -- 4. Burn the Token
        UPDATE EntryQrTokens 
        SET ConsumedAt = GETUTCDATE() 
        WHERE EntryQrTokenId = @TokenId;

        -- 5. Create the Session
        DECLARE @NewSessionId INT;
        INSERT INTO Sessions (UserId, StoreId, EntryQrTokenId, SessionStatusId, StartedAt)
        VALUES (@UserId, @StoreId, @TokenId, 1, GETUTCDATE());
        
        SET @NewSessionId = SCOPE_IDENTITY();
        SELECT * FROM Carts
        -- 6. Create the empty Cart
        DECLARE @NewCartId INT;
        INSERT INTO Carts (SessionId,UserId, CreatedAt, CartVersion)
        VALUES (@NewSessionId,@UserId ,GETUTCDATE(), 0);
        
        SET @NewCartId = SCOPE_IDENTITY();

        -- 7. Return the IDs for the system handshake
        SET @P_JSON_RESPONSE = (
            SELECT 
                @NewSessionId AS SessionId,
                @NewCartId AS CartId,
                'Access Granted' AS [Message]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_TopUpWallet]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT, @Amount DECIMAL(18,2);

    -- Extract from JSON
    SELECT 
        @UserId = UserId,
        @Amount = Amount
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        UserId INT '$.UserId',
        Amount DECIMAL(18,2) '$.Amount'
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @WalletId INT, @CurrentBalance DECIMAL(18,2), @NewBalance DECIMAL(18,2);

        -- 1. Lock the wallet row to prevent race conditions during read/write
        SELECT @WalletId = WalletId, @CurrentBalance = CurrentBalance
        FROM Wallets WITH (UPDLOCK)
        WHERE UserId = @UserId;

        IF @WalletId IS NULL
        BEGIN
            ;THROW 50002, 'Wallet not found for this user.', 1;
        END

        -- 2. Calculate new balance
        SET @NewBalance = @CurrentBalance + @Amount;

        -- 3. Update the Wallet
        UPDATE Wallets 
        SET CurrentBalance = @NewBalance, 
            LastUpdatedAt = GETUTCDATE()
        WHERE WalletId = @WalletId;
        -- 4. Create the Audit Trail (Ledger)
        INSERT INTO WalletLedgerEntries (WalletId, LedgerEntryTypeId, Amount, BalanceAfter, CreatedAt)
        VALUES (@WalletId, 1, @Amount, @NewBalance, GETUTCDATE());
        -- 5. Return the new state
        SET @P_JSON_RESPONSE = (
            SELECT 
                @WalletId AS WalletId,
                @NewBalance AS NewBalance,
                'Wallet topped up successfully.' AS [Message]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetWalletBalance]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        WalletId, 
        CurrentBalance, 
        LastUpdatedAt
    FROM Wallets
    WHERE UserId = @UserId
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;
END
--------------------------------------------------------------------------------------------------
GO
IF OBJECT_ID('dbo.SP_BindSessionTrack', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[SP_BindSessionTrack];
GO
CREATE PROCEDURE [dbo].[SP_BindSessionTrack]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT  -- Added to match your SqlExecutor
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. Extract properties natively from the incoming JSON DTO
        DECLARE @SessionId NVARCHAR(50);
        DECLARE @TrackId NVARCHAR(50);
        DECLARE @Source NVARCHAR(100);

        SELECT 
            @SessionId = [SessionId],
            @TrackId = [TrackId],
            @Source = [Source]
        FROM OPENJSON(@P_JSON_REQUEST)
        WITH (
            [SessionId] NVARCHAR(50),
            [TrackId] NVARCHAR(50),
            [Source] NVARCHAR(100)
        );

        -- 2. Begin Transaction to ensure atomic binding
        BEGIN TRAN;
        -- Retire previous tracks for this session
        UPDATE SessionTrackBindings
        SET IsCurrent = 0
        WHERE SessionId = @SessionId AND IsCurrent = 1;

        -- 3. Insert the new physical-to-digital link
        DECLARE @NewBindingId INT;
        DECLARE @BoundAt DATETIME2 = SYSUTCDATETIME();

        INSERT INTO SessionTrackBindings 
            (SessionId, TrackId, Source, BoundAt, IsCurrent)
        VALUES 
            (@SessionId, @TrackId, @Source, @BoundAt, 1);

        SET @NewBindingId = SCOPE_IDENTITY();

        COMMIT TRAN;

        -- 4. Assign the JSON directly to the OUTPUT parameter your executor is waiting for
        SET @P_JSON_RESPONSE = (
            SELECT 
                BindingId = @NewBindingId,
                SessionId = @SessionId,
                TrackId = @TrackId,
                BoundAt = @BoundAt,
                IsCurrent = CAST(1 AS BIT)
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
        );

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
--------------------------------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE SP_ProcessVisionEvent
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- =========================================================================
    -- PHASE A: INGESTION & RESOLUTION (No Locks Yet)
    -- =========================================================================
    
    -- 1. Extract values directly from the DTO payload
    DECLARE @TrackId NVARCHAR(50), 
            @AiLabel NVARCHAR(120), 
            @Action NVARCHAR(10), 
            @EventTime DATETIME2, 
            @Confidence DECIMAL(5,4), 
            @CameraCode NVARCHAR(50);

    SELECT 
        @TrackId = [TrackId],
        @AiLabel = [AiLabel],
        @Action = [Action],
        @EventTime = [EventTime],
        @Confidence = [Confidence],
        @CameraCode = [CameraCode]
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        TrackId NVARCHAR(50),
        AiLabel NVARCHAR(120),
        Action NVARCHAR(10),
        EventTime DATETIME2,
        Confidence DECIMAL(5,4),
        CameraCode NVARCHAR(50)
    );
    -- 2. Lookups (Camera, Store, Zone, Product, Session, Cart)
    DECLARE @CameraId INT, @StoreId INT, @ZoneId INT;
    SELECT @CameraId = CameraId, @StoreId = StoreId, @ZoneId = ZoneId 
    FROM Cameras WHERE CameraCode = @CameraCode;


    DECLARE @ProductId INT, @UnitPrice DECIMAL(18,2);
    SELECT @ProductId = Products.ProductId, @UnitPrice = PriceGross + PriceGross * VAT_Rate
    FROM Products INNER JOIN ProductAiLabels 
    ON Products.ProductId = ProductAiLabels.ProductId
    WHERE AiLabel = @AiLabel;


    DECLARE @SessionId INT, @CartId INT;
    -- Find the active session for this person
    SELECT @SessionId = SessionId 
    FROM SessionTrackBindings 
    WHERE TrackId = @TrackId;


    IF @SessionId IS NOT NULL
    BEGIN
        -- Find the active cart (StatusId = 1) for this session
        SELECT @CartId = CartId 
        FROM Carts INNER JOIN Sessions
        ON Carts.SessionId = Sessions.SessionId
        WHERE Sessions.SessionId = @SessionId AND Sessions.SessionStatusId = 1; 

    END
    -- 3. THE QoS 1 IDEMPOTENCY GUARD
    -- If this exact event was already processed (network duplicate), exit immediately.
    IF EXISTS (
        SELECT 1 FROM VisionEventsRaw 
        WHERE TrackId = @TrackId 
          AND EventTime = @EventTime 
          AND AiLabel = @AiLabel
          
    )
    BEGIN
        -- Skip DB writes, jump straight to returning the current cart state
        GOTO OutputSignalRJson;
    END

    -- =========================================================================
    -- PHASE B: THE RAW LOG
    -- =========================================================================
    
    DECLARE @VisionEventId INT;
    -- Insert with ProcessingStatusId = 1 (Pending)
    INSERT INTO VisionEventsRaw (
        StoreId, CameraId, ZoneId, MatchedSessionId, 
        TrackId, AiLabel, Action, EventTime, 
        Confidence, PayloadJson,IngestedAt ,ProcessingStatusId
    )
    VALUES (
        @StoreId, @CameraId, @ZoneId, @SessionId, 
        @TrackId, @AiLabel, @Action, @EventTime, 
        @Confidence, @P_JSON_REQUEST,GETDATE(), 1
    );
    
    SET @VisionEventId = SCOPE_IDENTITY();

    -- =========================================================================
    -- PHASE C: THE ATOMIC CART UPDATE
    -- =========================================================================
    
    -- Only proceed with financial cart updates if we successfully mapped the user and product
    IF @CartId IS NOT NULL AND @ProductId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            DECLARE @CartStateChanged BIT = 0;

            -- Handle Pick (Add 1 or Insert)
            IF @Action = 'Pick'
            BEGIN
                IF EXISTS (SELECT 1 FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId)
                BEGIN
                    UPDATE CartItems 
                    SET Quantity = Quantity + 1 
                    WHERE CartId = @CartId AND ProductId = @ProductId;
                END
                ELSE
                BEGIN 
                    INSERT INTO CartItems (CartId, ProductId,LastEventId, Quantity,LastAction) 
                    VALUES (@CartId, @ProductId, @VisionEventId ,1, @Action);
                END
                SET @CartStateChanged = 1;
            END
            
            -- Handle Return (Subtract 1, Delete if 0)
            ELSE IF @Action = 'Return'
            BEGIN
                IF EXISTS (SELECT 1 FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId)
                BEGIN
                    UPDATE CartItems 
                    SET Quantity = Quantity - 1 ,UpdatedAt = GETUTCDATE()
                    WHERE CartId = @CartId AND ProductId = @ProductId;
                    
                    -- Crucial: Remove row entirely if quantity hits zero
                    DELETE FROM CartItems 
                    WHERE CartId = @CartId AND ProductId = @ProductId AND Quantity <= 0;
                    SET @CartStateChanged = 1;
                END
            END
            IF @CartStateChanged = 1
                BEGIN
                    DECLARE @NewCartVersion INT;
                    SELECT @NewCartVersion = CartVersion + 1 FROM Carts WHERE CartId = @CartId;
                    -- Audit Trail
                    INSERT INTO CartItemEvents(CartId, ProductId, VisionEventId,Action, DeltaQty,CartVersionAfter, AppliedAt)
                    VALUES (@CartId, @ProductId,@VisionEventId, @Action, CASE WHEN @Action = 'Pick' THEN 1 ELSE -1 END,@NewCartVersion ,@EventTime);

                    -- Version Bump
                    UPDATE Carts 
                    SET CartVersion = @NewCartVersion, 
                        LastUpdatedAt = GETUTCDATE() 
                    WHERE CartId = @CartId;
                END

            -- Mark Raw Log as Applied (ProcessingStatusId = 2)
            UPDATE VisionEventsRaw 
            SET ProcessingStatusId = 2 
            WHERE VisionEventId = @VisionEventId;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW; -- Re-throw the error to the ASP.NET Global Exception Handler
        END CATCH
    END

    -- =========================================================================
    -- PHASE D: THE SIGNALR OUTPUT
    -- =========================================================================
    
    OutputSignalRJson:

    -- If no CartId exists (e.g., event was mapped to a ghost track), return empty JSON
    IF @CartId IS NULL
    BEGIN
        SET @P_JSON_RESPONSE = '{}';
        RETURN;
    END

    -- Return the freshly updated Cart, along with its Nested Items and calculated totals.
    -- We use WITHOUT_ARRAY_WRAPPER to return a single object, and INCLUDE_NULL_VALUES for Flutter safety.
    SET @P_JSON_RESPONSE =
    (SELECT 
        c.CartId,
        c.SessionId,
        c.CartVersion,
        c.LastUpdatedAt,
-- 1. FIX: Wrap SUM in ISNULL to return 0 instead of NULL
        ISNULL(( 
            SELECT SUM(ci.Quantity * (p.PriceGross + (p.PriceGross * p.VAT_Rate))) 
            FROM CartItems ci
            INNER JOIN Products p ON ci.ProductId = p.ProductId
            WHERE ci.CartId = c.CartId
        ), 0.00) AS CartTotal,
        
        -- 2. FIX: Wrap FOR JSON in ISNULL and use JSON_QUERY to return an empty array []
        ISNULL((
            SELECT 
                ci.ProductId,
                p.Name AS ProductName,
                pi.AiLabel,
                ci.Quantity,
                (p.PriceGross + (p.PriceGross * p.VAT_Rate)) AS UnitPrice,
                (ci.Quantity * (p.PriceGross + (p.PriceGross * p.VAT_Rate))) AS LineTotal
            FROM CartItems ci
            INNER JOIN Products p ON ci.ProductId = p.ProductId
            INNER JOIN ProductAiLabels Pi ON p.ProductId = pi.ProductId  AND pi.IsPrimary = 1
            WHERE ci.CartId = c.CartId
            FOR JSON PATH
        ), JSON_QUERY('[]')) AS CartItems
    FROM Carts c
    WHERE c.CartId = @CartId
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);

END
GO