using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BikeMateDbContext))]
    [Migration("20260630163000_RemoveRemainingSeedData")]
    public partial class RemoveRemainingSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @RequiredAdminEmail nvarchar(255) = N'isaiahandreinoda@gmail.com';

                DECLARE @SeedUserIds TABLE ([UserId] int PRIMARY KEY);
                INSERT INTO @SeedUserIds ([UserId])
                SELECT [u].[UserId]
                FROM [dbo].[users] AS [u]
                WHERE [u].[Email] <> @RequiredAdminEmail
                  AND (
                      [u].[UserId] IN (1, 2, 3, 101, 102, 103, 104, 111, 112, 113, 114, 115, 116, 117, 118)
                      OR [u].[Email] LIKE N'%@bikemate.test'
                  );

                DECLARE @SeedClientIds TABLE ([ClientId] int PRIMARY KEY);
                INSERT INTO @SeedClientIds ([ClientId])
                SELECT [c].[ClientId]
                FROM [dbo].[clients] AS [c]
                WHERE [c].[ClientId] = 1
                   OR [c].[UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DECLARE @SeedMechanicIds TABLE ([MechanicId] int PRIMARY KEY);
                INSERT INTO @SeedMechanicIds ([MechanicId])
                SELECT [m].[MechanicId]
                FROM [dbo].[mechanics] AS [m]
                WHERE [m].[MechanicId] IN (1, 101, 102, 103, 104, 105, 106, 107, 108)
                   OR [m].[UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DECLARE @SeedShopIds TABLE ([ShopId] int PRIMARY KEY);
                INSERT INTO @SeedShopIds ([ShopId])
                SELECT [s].[ShopId]
                FROM [dbo].[shops] AS [s]
                WHERE [s].[ShopId] IN (1, 101, 102, 103, 104)
                   OR [s].[OwnerUserId] IN (SELECT [UserId] FROM @SeedUserIds)
                   OR [s].[ShopName] IN (
                       N'BikeMate Partner Shop',
                       N'Southside MotoCare San Pedro',
                       N'Alabang CycleWorks',
                       N'Las Pinas MotoLab',
                       N'RoadReady Garage Binan'
                   );

                DECLARE @SeedShopServiceIds TABLE ([ShopServiceId] int PRIMARY KEY);
                INSERT INTO @SeedShopServiceIds ([ShopServiceId])
                SELECT [ss].[ShopServiceId]
                FROM [dbo].[shop_services] AS [ss]
                WHERE [ss].[ShopServiceId] IN (1, 2, 3, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121)
                   OR [ss].[ShopId] IN (SELECT [ShopId] FROM @SeedShopIds);

                DECLARE @SeedMotorcycleIds TABLE ([MotorcycleId] int PRIMARY KEY);
                INSERT INTO @SeedMotorcycleIds ([MotorcycleId])
                SELECT [mo].[MotorcycleId]
                FROM [dbo].[motorcycles] AS [mo]
                WHERE [mo].[MotorcycleId] = 1
                   OR [mo].[ClientId] IN (SELECT [ClientId] FROM @SeedClientIds);

                DECLARE @SeedRequestIds TABLE ([RequestId] int PRIMARY KEY);
                INSERT INTO @SeedRequestIds ([RequestId])
                SELECT [sr].[RequestId]
                FROM [dbo].[service_requests] AS [sr]
                WHERE [sr].[RequestId] IN (1, 2)
                   OR [sr].[ClientId] IN (SELECT [ClientId] FROM @SeedClientIds)
                   OR [sr].[ShopId] IN (SELECT [ShopId] FROM @SeedShopIds)
                   OR [sr].[ShopServiceId] IN (SELECT [ShopServiceId] FROM @SeedShopServiceIds)
                   OR [sr].[MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds)
                   OR [sr].[MotorcycleId] IN (SELECT [MotorcycleId] FROM @SeedMotorcycleIds);

                DECLARE @SeedPaymentIds TABLE ([PaymentId] int PRIMARY KEY);
                INSERT INTO @SeedPaymentIds ([PaymentId])
                SELECT [p].[PaymentId]
                FROM [dbo].[payments] AS [p]
                WHERE [p].[PaymentId] = 1
                   OR [p].[RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds)
                   OR [p].[ClientId] IN (SELECT [ClientId] FROM @SeedClientIds)
                   OR [p].[CheckoutUrl] = N'https://checkout.paymongo.com/test/bikemate-sample'
                   OR [p].[ProviderReferenceNumber] = N'BM-PAID-0001';

                DECLARE @SeedConversationIds TABLE ([ConversationId] int PRIMARY KEY);
                INSERT INTO @SeedConversationIds ([ConversationId])
                SELECT [c].[ConversationId]
                FROM [dbo].[conversations] AS [c]
                WHERE [c].[ConversationId] = 1
                   OR [c].[RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds);

                INSERT INTO @SeedConversationIds ([ConversationId])
                SELECT DISTINCT [cp].[ConversationId]
                FROM [dbo].[conversation_participants] AS [cp]
                WHERE [cp].[UserId] IN (SELECT [UserId] FROM @SeedUserIds)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM @SeedConversationIds AS [ids]
                      WHERE [ids].[ConversationId] = [cp].[ConversationId]);

                DELETE FROM [dbo].[conversation_participants]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @SeedConversationIds)
                   OR [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[messages]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @SeedConversationIds)
                   OR [SenderUserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[payment_events]
                WHERE [PaymentId] IN (SELECT [PaymentId] FROM @SeedPaymentIds);

                DELETE FROM [dbo].[payments]
                WHERE [PaymentId] IN (SELECT [PaymentId] FROM @SeedPaymentIds)
                   OR [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @SeedClientIds);

                DELETE FROM [dbo].[request_status_history]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds)
                   OR [ChangedByUserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[request_media]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds);

                DELETE FROM [dbo].[reviews]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @SeedClientIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds);

                DELETE FROM [dbo].[live_locations]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds)
                   OR [LiveLocationId] = 1;

                DELETE FROM [dbo].[notifications]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds)
                   OR [NotificationId] = 1;

                DELETE FROM [dbo].[conversations]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @SeedConversationIds);

                DELETE FROM [dbo].[service_requests]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @SeedRequestIds);

                DELETE FROM [dbo].[service_images]
                WHERE [ShopServiceId] IN (SELECT [ShopServiceId] FROM @SeedShopServiceIds);

                DELETE FROM [dbo].[product_images]
                WHERE [ProductId] IN (1, 2)
                   OR [ProductId] IN (
                       SELECT [ProductId]
                       FROM [dbo].[products]
                       WHERE [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds));

                DELETE FROM [dbo].[products]
                WHERE [ProductId] IN (1, 2)
                   OR [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds);

                DELETE FROM [dbo].[shop_mechanics]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds);

                DELETE FROM [dbo].[shop_services]
                WHERE [ShopServiceId] IN (SELECT [ShopServiceId] FROM @SeedShopServiceIds)
                   OR [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds);

                DELETE FROM [dbo].[shop_operating_hours]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds);

                DELETE FROM [dbo].[client_addresses]
                WHERE [AddressId] = 1
                   OR [ClientId] IN (SELECT [ClientId] FROM @SeedClientIds);

                DELETE FROM [dbo].[motorcycles]
                WHERE [MotorcycleId] IN (SELECT [MotorcycleId] FROM @SeedMotorcycleIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @SeedClientIds);

                DELETE FROM [dbo].[mechanic_availability]
                WHERE [MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds);

                DELETE FROM [dbo].[mechanics]
                WHERE [MechanicId] IN (SELECT [MechanicId] FROM @SeedMechanicIds);

                DELETE FROM [dbo].[clients]
                WHERE [ClientId] IN (SELECT [ClientId] FROM @SeedClientIds);

                DELETE FROM [dbo].[shops]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @SeedShopIds);

                DELETE FROM [dbo].[audit_logs]
                WHERE [ActorUserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[otp_verifications]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[password_reset_tokens]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[user_device_tokens]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[user_auth_providers]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[user_roles]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);

                DELETE FROM [dbo].[users]
                WHERE [UserId] IN (SELECT [UserId] FROM @SeedUserIds);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
