using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSystemAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @AdminEmail nvarchar(255) = N'isaiahandreinoda@gmail.com';
                DECLARE @AdminPasswordHash nvarchar(500) = N'sha256:588c55f3ce2b8569b153c5abbf13f9f74308b88a20017cc699b835cc93195d16';
                DECLARE @AdminUserId int;
                DECLARE @SystemAdminRoleId int;

                SELECT @SystemAdminRoleId = [RoleId]
                FROM [dbo].[roles]
                WHERE [RoleName] = N'SystemAdmin';

                IF @SystemAdminRoleId IS NULL
                BEGIN
                    SET IDENTITY_INSERT [dbo].[roles] ON;

                    IF NOT EXISTS (SELECT 1 FROM [dbo].[roles] WHERE [RoleId] = 4)
                    BEGIN
                        INSERT INTO [dbo].[roles] ([RoleId], [RoleName])
                        VALUES (4, N'SystemAdmin');
                    END

                    SET IDENTITY_INSERT [dbo].[roles] OFF;
                    SELECT @SystemAdminRoleId = [RoleId]
                    FROM [dbo].[roles]
                    WHERE [RoleName] = N'SystemAdmin';
                END

                SELECT @AdminUserId = [UserId]
                FROM [dbo].[users]
                WHERE [Email] = @AdminEmail;

                IF @AdminUserId IS NULL
                BEGIN
                    INSERT INTO [dbo].[users]
                        ([FirstName], [LastName], [Email], [PhoneNumber], [PasswordHash], [ProfileImageUrl], [EmailVerified], [PhoneVerified], [AccountStatus], [CreatedAt], [UpdatedAt])
                    VALUES
                        (N'Isaiah', N'Noda', @AdminEmail, N'+639170000001', @AdminPasswordHash, NULL, 1, 1, N'active', SYSUTCDATETIME(), NULL);

                    SET @AdminUserId = CONVERT(int, SCOPE_IDENTITY());
                END
                ELSE
                BEGIN
                    UPDATE [dbo].[users]
                    SET [FirstName] = CASE WHEN NULLIF(LTRIM(RTRIM([FirstName])), N'') IS NULL THEN N'Isaiah' ELSE [FirstName] END,
                        [LastName] = CASE WHEN NULLIF(LTRIM(RTRIM([LastName])), N'') IS NULL THEN N'Noda' ELSE [LastName] END,
                        [PasswordHash] = @AdminPasswordHash,
                        [EmailVerified] = 1,
                        [PhoneVerified] = 1,
                        [AccountStatus] = N'active',
                        [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [UserId] = @AdminUserId;
                END

                IF @SystemAdminRoleId IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM [dbo].[user_roles]
                       WHERE [UserId] = @AdminUserId
                         AND [RoleId] = @SystemAdminRoleId)
                BEGIN
                    INSERT INTO [dbo].[user_roles] ([UserId], [RoleId], [AssignedAt])
                    VALUES (@AdminUserId, @SystemAdminRoleId, SYSUTCDATETIME());
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the admin account on rollback to avoid locking operators out.
        }
    }
}
