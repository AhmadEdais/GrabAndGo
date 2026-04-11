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
    DECLARE @ExpiresAt DATETIME2 = DATEADD(second, 30, @IssuedAt); -- FIX: Corrected to ExpiresAt

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