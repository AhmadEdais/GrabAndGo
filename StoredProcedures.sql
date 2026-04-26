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
    DECLARE @IssuedAt DATETIME2 = GETDATE();
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
        SET ConsumedAt = GETDATE() 
        WHERE EntryQrTokenId = @TokenId;

        -- 5. Create the Session
        DECLARE @NewSessionId INT;
        INSERT INTO Sessions (UserId, StoreId, EntryQrTokenId, SessionStatusId, StartedAt)
        VALUES (@UserId, @StoreId, @TokenId, 1, GETDATE());
        
        SET @NewSessionId = SCOPE_IDENTITY();
        SELECT * FROM Carts
        -- 6. Create the empty Cart
        DECLARE @NewCartId INT;
        INSERT INTO Carts (SessionId,UserId, CreatedAt, CartVersion)
        VALUES (@NewSessionId,@UserId ,GETDATE(), 0);
        
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
            LastUpdatedAt = GETDATE()
        WHERE WalletId = @WalletId;
        -- 4. Create the Audit Trail (Ledger)
        INSERT INTO WalletLedgerEntries (WalletId, LedgerEntryTypeId, Amount, BalanceAfter, CreatedAt)
        VALUES (@WalletId, 1, @Amount, @NewBalance, GETDATE());
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
                    SET Quantity = Quantity - 1 ,UpdatedAt = GETDATE()
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
                        LastUpdatedAt = GETDATE() 
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
            THROW; 
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

    -- 1. Pre-calculate the Cart Total and Wallet Balance for performance
    DECLARE @CurrentCartTotal DECIMAL(18,2) = 0.00;
    
    SELECT @CurrentCartTotal = ISNULL(SUM(ci.Quantity * (p.PriceGross + (p.PriceGross * p.VAT_Rate))), 0.00)
    FROM CartItems ci
    INNER JOIN Products p ON ci.ProductId = p.ProductId
    WHERE ci.CartId = @CartId;

    DECLARE @CurrentBalance DECIMAL(18,2) = 0.00;

    -- We already have @SessionId, so we join to find the exact User's Wallet
    SELECT @CurrentBalance = w.CurrentBalance 
    FROM Wallets w 
    INNER JOIN Sessions s ON w.UserId = s.UserId 
    WHERE s.SessionId = @SessionId;

    -- 2. Determine Shortfall Logic
    DECLARE @IsShortfall BIT = CASE WHEN @CurrentCartTotal > @CurrentBalance THEN 1 ELSE 0 END;
    DECLARE @ShortfallAmount DECIMAL(18,2) = CASE WHEN @IsShortfall = 1 THEN (@CurrentCartTotal - @CurrentBalance) ELSE 0.00 END;

    SET @P_JSON_RESPONSE =
    (SELECT 
        c.CartId,
        c.SessionId,
        c.CartVersion,
        c.LastUpdatedAt,

        @CurrentBalance AS WalletBalance,
        @CurrentCartTotal AS CartTotal,
        @IsShortfall AS IsShortfall,
        @ShortfallAmount AS ShortfallAmount,
        
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
--------------------------------------------------------------------------------------------------
GO
USE [GrabAndGoDB]
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_ProcessCheckout]
    @P_JSON_REQUEST NVARCHAR(MAX),
    @P_JSON_RESPONSE NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- =========================================================================
    -- PHASE 1: PARSE INPUT & RESOLVE DIGITAL IDENTITY
    -- =========================================================================
    DECLARE @TrackId NVARCHAR(50), 
            @CameraCode NVARCHAR(50), 
            @EventTime DATETIME2;

    SELECT 
        @TrackId = [TrackId],
        @CameraCode = [CameraCode],
        @EventTime = [EventTime]
    FROM OPENJSON(@P_JSON_REQUEST)
    WITH (
        TrackId NVARCHAR(50),
        CameraCode NVARCHAR(50),
        EventTime DATETIME2
    );

    -- Find the Active Session tied to this TrackId
    DECLARE @SessionId INT, @UserId INT, @StoreId INT, @CartId INT;

    SELECT 
        @SessionId = s.SessionId,
        @UserId = s.UserId,
        @StoreId = s.StoreId
    FROM SessionTrackBindings stb
    INNER JOIN Sessions s ON stb.SessionId = s.SessionId
    WHERE stb.TrackId = @TrackId 
      AND stb.IsCurrent = 1 
      AND s.SessionStatusId = 1; -- 1 = Active
    -- If no active session is found for this person, safely reject the gate opening
    IF @SessionId IS NULL
    BEGIN
        SET @P_JSON_RESPONSE = '{"GateAction":"KeepClosed", "Message":"No active shopping session found.", "IsSuccess":false, "ShortfallAmount":0}';
        RETURN;
    END

    -- Find the active Cart
    SELECT @CartId = CartId FROM Carts WHERE SessionId = @SessionId;

    -- =========================================================================
    -- PHASE 2: ATOMIC FINANCIAL CALCULATIONS & TRANSACTIONS
    -- =========================================================================
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Lock the Wallet to prevent double-spending race conditions
        DECLARE @WalletId INT, @CurrentBalance DECIMAL(18,2);
        SELECT @WalletId = WalletId, @CurrentBalance = CurrentBalance
        FROM Wallets WITH (UPDLOCK)
        WHERE UserId = @UserId;

        -- 2. Calculate Final Cart Totals
        DECLARE @Subtotal DECIMAL(18,2) = 0.00, 
                @TaxTotal DECIMAL(18,2) = 0.00, 
                @GrandTotal DECIMAL(18,2) = 0.00;

        SELECT 
            @Subtotal = ISNULL(SUM(ci.Quantity * p.PriceGross), 0.00),
            @TaxTotal = ISNULL(SUM(ci.Quantity * (p.PriceGross * p.VAT_Rate)), 0.00)
        FROM CartItems ci
        INNER JOIN Products p ON ci.ProductId = p.ProductId
        WHERE ci.CartId = @CartId;

        SET @GrandTotal = @Subtotal + @TaxTotal;

        -- 3. The HARD STOP Check (Insufficient Funds)
        IF @CurrentBalance < @GrandTotal
        BEGIN
            -- User does not have enough money. We COMMIT to release the lock, 
            -- but we DO NOT process the checkout. Session remains active.
            DECLARE @Shortfall DECIMAL(18,2) = @GrandTotal - @CurrentBalance;
            
            SET @P_JSON_RESPONSE = (
                SELECT 
                    'KeepClosed' AS GateAction,
                    'Insufficient Funds. Please Top Up.' AS Message,
                    CAST(0 AS BIT) AS IsSuccess,
                    @Shortfall AS ShortfallAmount,
                    @SessionId AS SessionId
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );
            COMMIT TRANSACTION;
            RETURN;
        END

        -- =====================================================================
        -- PHASE 3: CHECKOUT EXECUTION (User has enough funds)
        -- =====================================================================

        DECLARE @NewBalance DECIMAL(18,2) = @CurrentBalance - @GrandTotal;

        -- A. Deduct Wallet Funds
        UPDATE Wallets 
        SET CurrentBalance = @NewBalance, LastUpdatedAt = GETDATE()
        WHERE WalletId = @WalletId;

        -- B. Create Header Transaction (Status = 2 / Completed)
        DECLARE @TransactionId INT;
        INSERT INTO Transactions (SessionId, UserId, CartId, Subtotal, Tax, Total, PaymentStatusId, CreatedAt)
        VALUES (@SessionId, @UserId,@CartId, @Subtotal,@TaxTotal, @GrandTotal, 2,GETDATE() );

        SET @TransactionId = SCOPE_IDENTITY();

        -- C. Snapshot CartItems into TransactionItems (The Receipt Lines)
        INSERT INTO TransactionItems (TransactionId, ProductId, Quantity, UnitPrice, LineTotal)
        SELECT 
            @TransactionId, 
            ci.ProductId, 
            ci.Quantity, 
            (p.PriceGross + (p.PriceGross * p.VAT_Rate)), 
            (ci.Quantity * (p.PriceGross + (p.PriceGross * p.VAT_Rate)))
        FROM CartItems ci
        INNER JOIN Products p ON ci.ProductId = p.ProductId
        WHERE ci.CartId = @CartId;

        DECLARE @LedgerReference NVARCHAR(100) = 'Checkout_Txn_' + CAST(@TransactionId AS NVARCHAR(50));
        -- D. Write Ledger Entry (Immutable Financial Record)
        INSERT INTO WalletLedgerEntries (WalletId, LedgerEntryTypeId, Amount, BalanceAfter, RelatedTransactionId, CreatedAt,Reference)
        VALUES (@WalletId, 2, -@GrandTotal, @NewBalance, @TransactionId, GETDATE(),@LedgerReference); -- 2 = Debit

        -- E. Generate Stub for Invoice Generation (Phase 5 will fill the PDF URL later)
        INSERT INTO Invoices (TransactionId, GeneratedAt)
        VALUES (@TransactionId, GETDATE());

        -- F. Terminate the Digital Session
        UPDATE Sessions 
        SET SessionStatusId = 2, ExitDetectedAt = @EventTime, EndedAt = GETDATE() 
        WHERE SessionId = @SessionId;

        -- G. Unbind the physical Track ID so the camera stops associating them with the session
        UPDATE SessionTrackBindings 
        SET IsCurrent = 0 
        WHERE SessionId = @SessionId;

        -- H. Build Success JSON Response
        SET @P_JSON_RESPONSE = (
            SELECT 
                'OpenGate' AS GateAction,
                'Checkout Successful' AS Message,
                CAST(1 AS BIT) AS IsSuccess,
                0.00 AS ShortfallAmount,
                @SessionId AS SessionId,
                @TransactionId AS TransactionId,
                @GrandTotal AS TotalDeducted,
                @NewBalance AS RemainingBalance
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW; -- Pass exception back to C#
    END CATCH
END
GO
--------------------------------------------------------------------------------------------------
 USE [GrabAndGoDB]
  GO

  -- =============================================================================
  -- SP_GetInvoiceData
  -- Returns the full invoice payload needed to render a PDF, as a single JSON
  -- object, including nested line items.
  -- Consumer: InvoiceService (called by InvoiceWorker background service, and by
  -- GET /api/invoices/{transactionId} controller endpoint).
  -- =============================================================================
  CREATE OR ALTER PROCEDURE dbo.SP_GetInvoiceData
      @TransactionId INT
  AS
  BEGIN
      SET NOCOUNT ON;

      SELECT
          i.InvoiceId,
          i.TransactionId,
          i.PdfUrlOrPath,
          i.GeneratedAt,

          t.Subtotal,
          t.Tax,
          t.Total,
          t.CreatedAt,
          t.CompletedAt,

          ps.StatusName       AS PaymentStatus,

          u.UserId            AS CustomerUserId,
          u.FirstName         AS CustomerFirstName,
          u.LastName          AS CustomerLastName,
          u.Email             AS CustomerEmail,

          s.StoreId,
          s.StoreCode,
          s.Name              AS StoreName,
          s.Timezone          AS StoreTimezone,

          Items = (
              SELECT
                  ti.TransactionItemId,
                  ti.ProductId,
                  p.Name      AS ProductName,
                  p.SKU,
                  ti.Quantity,
                  ti.UnitPrice,
                  ti.LineTotal,
                  p.VAT_Rate
              FROM dbo.TransactionItems ti
              INNER JOIN dbo.Products p ON p.ProductId = ti.ProductId
              WHERE ti.TransactionId = i.TransactionId
              ORDER BY ti.TransactionItemId
              FOR JSON PATH, INCLUDE_NULL_VALUES
          )

      FROM dbo.Invoices i
          INNER JOIN dbo.Transactions    t  ON t.TransactionId    = i.TransactionId
          INNER JOIN dbo.Sessions        ses ON ses.SessionId     = t.SessionId
          INNER JOIN dbo.Stores          s  ON s.StoreId          = ses.StoreId
          INNER JOIN dbo.Users           u  ON u.UserId           = t.UserId
          INNER JOIN dbo.PaymentStatuses ps ON ps.PaymentStatusId = t.PaymentStatusId
      WHERE i.TransactionId = @TransactionId
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;
  END
  GO


  -- =============================================================================
  -- SP_UpdateInvoicePath
  -- Writes the generated PDF path (or URL) back into Invoices.PdfUrlOrPath.
  -- Consumer: InvoiceService, after the PDF file has been persisted to storage.
  --
  -- Request JSON shape:
  --   { "TransactionId": <int>, "PdfUrlOrPath": "<string>" }
  --
  -- Response JSON shape:
  --   { "IsSuccess": <bit>, "Message": "<string>",
  --     "TransactionId": <int>, "PdfUrlOrPath": "<string>" }
  -- =============================================================================
  CREATE OR ALTER PROCEDURE dbo.SP_UpdateInvoicePath
      @P_JSON_REQUEST  NVARCHAR(MAX),
      @P_JSON_RESPONSE NVARCHAR(MAX) = NULL OUTPUT
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @TransactionId  INT;
      DECLARE @PdfUrlOrPath   NVARCHAR(500);

      SELECT
          @TransactionId = TransactionId,
          @PdfUrlOrPath  = PdfUrlOrPath
      FROM OPENJSON(@P_JSON_REQUEST)
      WITH (
          TransactionId INT            '$.TransactionId',
          PdfUrlOrPath  NVARCHAR(500)  '$.PdfUrlOrPath'
      );

      -- Guard: invoice stub must exist before we can fill in the path.
      IF NOT EXISTS (
          SELECT 1 FROM dbo.Invoices WHERE TransactionId = @TransactionId
      )
      BEGIN
          SET @P_JSON_RESPONSE = (
              SELECT
                  CAST(0 AS BIT)                                                 AS IsSuccess,
                  'No invoice stub found for the specified TransactionId.'       AS Message,
                  @TransactionId                                                 AS TransactionId,
                  CAST(NULL AS NVARCHAR(500))                                    AS PdfUrlOrPath
              FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
          );
          RETURN;
      END

      -- Guard: don't overwrite an existing PDF path silently. The worker should
      -- only pick up rows where PdfUrlOrPath IS NULL, but defending against
      -- double-submission here keeps the invariant enforced at the data layer.
      IF EXISTS (
          SELECT 1 FROM dbo.Invoices
          WHERE TransactionId = @TransactionId AND PdfUrlOrPath IS NOT NULL
      )
      BEGIN
          SET @P_JSON_RESPONSE = (
              SELECT
                  CAST(0 AS BIT)                                                 AS IsSuccess,
                  'PDF path is already set for this invoice.'                    AS Message,
                  @TransactionId                                                 AS TransactionId,
                  (SELECT PdfUrlOrPath FROM dbo.Invoices
                    WHERE TransactionId = @TransactionId)                        AS PdfUrlOrPath
              FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
          );
          RETURN;
      END

      UPDATE dbo.Invoices
      SET    PdfUrlOrPath = @PdfUrlOrPath
      WHERE  TransactionId = @TransactionId;

      SET @P_JSON_RESPONSE = (
          SELECT
              CAST(1 AS BIT)                                                     AS IsSuccess,
              'Invoice PDF path stored successfully.'                            AS Message,
              @TransactionId                                                     AS TransactionId,
              @PdfUrlOrPath                                                      AS PdfUrlOrPath
          FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
      );
  END
  GO
   CREATE OR ALTER PROCEDURE dbo.SP_GetPendingInvoices
      @BatchSize INT = 10
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @CompletedStatusId INT = (
          SELECT PaymentStatusId FROM dbo.PaymentStatuses WHERE StatusName = 'Completed'
      );

      SELECT TOP (@BatchSize)
          i.TransactionId,
          i.GeneratedAt
      FROM dbo.Invoices i
          INNER JOIN dbo.Transactions t ON t.TransactionId = i.TransactionId
      WHERE i.PdfUrlOrPath IS NULL
        AND t.PaymentStatusId = @CompletedStatusId
      ORDER BY i.GeneratedAt ASC
      FOR JSON PATH, INCLUDE_NULL_VALUES;
  END
  GO
   USE [GrabAndGoDB];
  GO

  CREATE OR ALTER PROCEDURE dbo.SP_GetUserTransactions
      @UserId     INT,
      @PageNumber INT = 1,
      @PageSize   INT = 20
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

      SELECT
          t.TransactionId,
          s.StoreId,
          s.Name           AS StoreName,
          t.Subtotal,
          t.Tax,
          t.Total,
          ps.StatusName    AS PaymentStatus,
          t.CreatedAt,
          t.CompletedAt,
          ItemCount = (SELECT COUNT(1) FROM dbo.TransactionItems ti WHERE ti.TransactionId = t.TransactionId)
      FROM dbo.Transactions t
          INNER JOIN dbo.Sessions        ses ON ses.SessionId       = t.SessionId
          INNER JOIN dbo.Stores          s   ON s.StoreId           = ses.StoreId
          INNER JOIN dbo.PaymentStatuses ps  ON ps.PaymentStatusId  = t.PaymentStatusId
      WHERE t.UserId = @UserId
      ORDER BY t.CreatedAt DESC
      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
      FOR JSON PATH, INCLUDE_NULL_VALUES;
  END
  GO


  CREATE OR ALTER PROCEDURE dbo.SP_GetUserInvoices
      @UserId     INT,
      @PageNumber INT = 1,
      @PageSize   INT = 20
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

      SELECT
          i.InvoiceId,
          i.TransactionId,
          i.PdfUrlOrPath,
          i.GeneratedAt,
          t.Total,
          s.Name        AS StoreName,
          ps.StatusName AS PaymentStatus
      FROM dbo.Invoices i
          INNER JOIN dbo.Transactions    t   ON t.TransactionId    = i.TransactionId
          INNER JOIN dbo.Sessions        ses ON ses.SessionId      = t.SessionId
          INNER JOIN dbo.Stores          s   ON s.StoreId          = ses.StoreId
          INNER JOIN dbo.PaymentStatuses ps  ON ps.PaymentStatusId = t.PaymentStatusId
      WHERE t.UserId = @UserId
      ORDER BY i.GeneratedAt DESC
      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
      FOR JSON PATH, INCLUDE_NULL_VALUES;
  END
  GO
   USE [GrabAndGoDB];
  GO

  CREATE OR ALTER PROCEDURE dbo.SP_GetUserWalletLedger
      @UserId     INT,
      @PageNumber INT = 1,
      @PageSize   INT = 20
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

      SELECT
          wle.LedgerEntryId,
          wle.WalletId,
          wle.RelatedTransactionId,
          let.TypeName     AS EntryType,
          wle.Amount,
          wle.BalanceAfter,
          wle.CreatedAt,
          wle.Reference
      FROM dbo.WalletLedgerEntries wle
          INNER JOIN dbo.Wallets w ON w.WalletId = wle.WalletId
          INNER JOIN dbo.LedgerEntryTypes let ON let.LedgerEntryTypeId = wle.LedgerEntryTypeId
      WHERE w.UserId = @UserId
      ORDER BY wle.CreatedAt DESC, wle.LedgerEntryId DESC
      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
      FOR JSON PATH, INCLUDE_NULL_VALUES;
  END
  GO


  CREATE OR ALTER PROCEDURE dbo.SP_GetUserActiveSession
      @UserId INT
  AS
  BEGIN
      SET NOCOUNT ON;

      SELECT TOP 1
          s.SessionId,
          s.StoreId,
          st.StoreCode,
          st.Name           AS StoreName,
          s.StartedAt,
          c.CartId,
          c.CartVersion,
          ss.StatusName     AS SessionStatus
      FROM dbo.Sessions s
          INNER JOIN dbo.Stores st ON st.StoreId = s.StoreId
          INNER JOIN dbo.SessionStatuses ss ON ss.SessionStatusId = s.SessionStatusId
          LEFT  JOIN dbo.Carts c ON c.SessionId = s.SessionId
      WHERE s.UserId = @UserId
        AND s.EndedAt IS NULL
        AND ss.StatusName = 'Active'
      ORDER BY s.StartedAt DESC
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;
  END
  GO
  
  CREATE OR ALTER PROCEDURE dbo.SP_GetProducts
      @PageNumber INT             = 1,
      @PageSize   INT             = 50,
      @Search     NVARCHAR(200)   = NULL
  AS
  BEGIN
      SET NOCOUNT ON;

      DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

      SELECT
          p.ProductId,
          p.Name,
          p.SKU,
          p.PriceGross,
          p.VAT_Rate,
          p.ImageUrl,
          p.IsActive
      FROM dbo.Products p
      WHERE p.IsActive = 1
        AND (@Search IS NULL
             OR p.Name LIKE '%' + @Search + '%'
             OR p.SKU  LIKE '%' + @Search + '%')
      ORDER BY p.Name ASC
      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
      FOR JSON PATH, INCLUDE_NULL_VALUES;
  END
  GO