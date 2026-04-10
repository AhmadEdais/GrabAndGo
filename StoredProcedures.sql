USE [GrabAndGoDB]
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_InsertUser]
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @Email NVARCHAR(320),
    @PasswordHash NVARCHAR(256),
    @NewUserId INT OUTPUT 
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Check if email exists (Done outside the transaction to keep locks short)
    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        SET @NewUserId = -1; -- Signal to C# that this is a duplicate
        RETURN;
    END

    -- 2. Begin Transaction Block
    BEGIN TRY
        BEGIN TRANSACTION;

            -- A. Insert the User
            INSERT INTO [dbo].[Users] ([FirstName], [LastName], [Email], [PasswordHash])
            VALUES (@FirstName, @LastName, @Email, @PasswordHash);

            -- B. Capture the new UserId immediately
            SET @NewUserId = SCOPE_IDENTITY();

            -- C. Create a Wallet for the new User
            -- We only insert UserId; the DB defaults handle Balance, Currency, and Dates
            INSERT INTO [dbo].[Wallets] ([UserId])
            VALUES (@NewUserId);

        -- If everything succeeded, commit both inserts to the database
        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        -- If anything failed, rollback (undo) everything in this transaction
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        -- Signal C# that a database error occurred
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_GetUserByEmail]
    @Email NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;

    -- We select only the 3 specific columns needed for verification
    SELECT 
        UserId, 
        PasswordHash, 
        IsActive
    FROM [dbo].[Users]
    WHERE Email = @Email;
END
GO