using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemoSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @DemoUserIds TABLE ([UserId] int PRIMARY KEY);
                INSERT INTO @DemoUserIds ([UserId])
                VALUES (1), (2), (3), (4), (101), (102), (103), (104), (111), (112), (113), (114), (115), (116), (117), (118);

                DECLARE @DemoClientIds TABLE ([ClientId] int PRIMARY KEY);
                INSERT INTO @DemoClientIds ([ClientId]) VALUES (1);
                INSERT INTO @DemoClientIds ([ClientId])
                SELECT [c].[ClientId]
                FROM [dbo].[clients] AS [c]
                WHERE [c].[UserId] IN (SELECT [UserId] FROM @DemoUserIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoClientIds AS [ids] WHERE [ids].[ClientId] = [c].[ClientId]);

                DECLARE @DemoMechanicIds TABLE ([MechanicId] int PRIMARY KEY);
                INSERT INTO @DemoMechanicIds ([MechanicId])
                VALUES (1), (101), (102), (103), (104), (105), (106), (107), (108);
                INSERT INTO @DemoMechanicIds ([MechanicId])
                SELECT [m].[MechanicId]
                FROM [dbo].[mechanics] AS [m]
                WHERE [m].[UserId] IN (SELECT [UserId] FROM @DemoUserIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoMechanicIds AS [ids] WHERE [ids].[MechanicId] = [m].[MechanicId]);

                DECLARE @DemoShopIds TABLE ([ShopId] int PRIMARY KEY);
                INSERT INTO @DemoShopIds ([ShopId]) VALUES (1), (101), (102), (103), (104);
                INSERT INTO @DemoShopIds ([ShopId])
                SELECT [s].[ShopId]
                FROM [dbo].[shops] AS [s]
                WHERE [s].[OwnerUserId] IN (SELECT [UserId] FROM @DemoUserIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoShopIds AS [ids] WHERE [ids].[ShopId] = [s].[ShopId]);

                DECLARE @DemoShopServiceIds TABLE ([ShopServiceId] int PRIMARY KEY);
                INSERT INTO @DemoShopServiceIds ([ShopServiceId])
                VALUES (1), (2), (3), (101), (102), (103), (104), (105), (106), (107), (108), (109), (110), (111), (112), (113), (114), (115), (116), (117), (118), (119), (120), (121);
                INSERT INTO @DemoShopServiceIds ([ShopServiceId])
                SELECT [ss].[ShopServiceId]
                FROM [dbo].[shop_services] AS [ss]
                WHERE [ss].[ShopId] IN (SELECT [ShopId] FROM @DemoShopIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoShopServiceIds AS [ids] WHERE [ids].[ShopServiceId] = [ss].[ShopServiceId]);

                DECLARE @DemoMotorcycleIds TABLE ([MotorcycleId] int PRIMARY KEY);
                INSERT INTO @DemoMotorcycleIds ([MotorcycleId]) VALUES (1);
                INSERT INTO @DemoMotorcycleIds ([MotorcycleId])
                SELECT [mo].[MotorcycleId]
                FROM [dbo].[motorcycles] AS [mo]
                WHERE [mo].[ClientId] IN (SELECT [ClientId] FROM @DemoClientIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoMotorcycleIds AS [ids] WHERE [ids].[MotorcycleId] = [mo].[MotorcycleId]);

                DECLARE @DemoRequestIds TABLE ([RequestId] int PRIMARY KEY);
                INSERT INTO @DemoRequestIds ([RequestId]) VALUES (1), (2);
                INSERT INTO @DemoRequestIds ([RequestId])
                SELECT [sr].[RequestId]
                FROM [dbo].[service_requests] AS [sr]
                WHERE ([sr].[ClientId] IN (SELECT [ClientId] FROM @DemoClientIds)
                    OR [sr].[ShopId] IN (SELECT [ShopId] FROM @DemoShopIds)
                    OR [sr].[ShopServiceId] IN (SELECT [ShopServiceId] FROM @DemoShopServiceIds)
                    OR [sr].[MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds)
                    OR [sr].[MotorcycleId] IN (SELECT [MotorcycleId] FROM @DemoMotorcycleIds))
                  AND NOT EXISTS (SELECT 1 FROM @DemoRequestIds AS [ids] WHERE [ids].[RequestId] = [sr].[RequestId]);

                DECLARE @DemoPaymentIds TABLE ([PaymentId] int PRIMARY KEY);
                INSERT INTO @DemoPaymentIds ([PaymentId]) VALUES (1);
                INSERT INTO @DemoPaymentIds ([PaymentId])
                SELECT [p].[PaymentId]
                FROM [dbo].[payments] AS [p]
                WHERE ([p].[RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds)
                    OR [p].[ClientId] IN (SELECT [ClientId] FROM @DemoClientIds))
                  AND NOT EXISTS (SELECT 1 FROM @DemoPaymentIds AS [ids] WHERE [ids].[PaymentId] = [p].[PaymentId]);

                DECLARE @DemoConversationIds TABLE ([ConversationId] int PRIMARY KEY);
                INSERT INTO @DemoConversationIds ([ConversationId])
                SELECT [ConversationId]
                FROM [dbo].[conversations]
                WHERE [ConversationId] = 1 OR [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds);
                INSERT INTO @DemoConversationIds ([ConversationId])
                SELECT [cp].[ConversationId]
                FROM [dbo].[conversation_participants] AS [cp]
                WHERE [cp].[UserId] IN (SELECT [UserId] FROM @DemoUserIds)
                  AND NOT EXISTS (SELECT 1 FROM @DemoConversationIds AS [ids] WHERE [ids].[ConversationId] = [cp].[ConversationId]);

                DELETE FROM [dbo].[conversation_participants]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @DemoConversationIds)
                   OR [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[messages]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @DemoConversationIds)
                   OR [SenderUserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[payment_events]
                WHERE [PaymentId] IN (SELECT [PaymentId] FROM @DemoPaymentIds);

                DELETE FROM [dbo].[payments]
                WHERE [PaymentId] IN (SELECT [PaymentId] FROM @DemoPaymentIds)
                   OR [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @DemoClientIds);

                DELETE FROM [dbo].[request_status_history]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds)
                   OR [ChangedByUserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[request_media]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds);

                DELETE FROM [dbo].[reviews]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @DemoClientIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds);

                DELETE FROM [dbo].[live_locations]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds)
                   OR [LiveLocationId] = 1;

                DELETE FROM [dbo].[notifications]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds)
                   OR [NotificationId] = 1;

                DELETE FROM [dbo].[conversations]
                WHERE [ConversationId] IN (SELECT [ConversationId] FROM @DemoConversationIds);

                DELETE FROM [dbo].[service_requests]
                WHERE [RequestId] IN (SELECT [RequestId] FROM @DemoRequestIds);

                DELETE FROM [dbo].[service_images]
                WHERE [ShopServiceId] IN (SELECT [ShopServiceId] FROM @DemoShopServiceIds);

                DELETE FROM [dbo].[product_images]
                WHERE [ProductId] IN (1, 2)
                   OR [ProductId] IN (
                       SELECT [ProductId]
                       FROM [dbo].[products]
                       WHERE [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds));

                DELETE FROM [dbo].[products]
                WHERE [ProductId] IN (1, 2)
                   OR [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds);

                DELETE FROM [dbo].[shop_mechanics]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds)
                   OR [MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds);

                DELETE FROM [dbo].[shop_services]
                WHERE [ShopServiceId] IN (SELECT [ShopServiceId] FROM @DemoShopServiceIds)
                   OR [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds);

                DELETE FROM [dbo].[shop_operating_hours]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds);

                DELETE FROM [dbo].[client_addresses]
                WHERE [AddressId] = 1
                   OR [ClientId] IN (SELECT [ClientId] FROM @DemoClientIds);

                DELETE FROM [dbo].[motorcycles]
                WHERE [MotorcycleId] IN (SELECT [MotorcycleId] FROM @DemoMotorcycleIds)
                   OR [ClientId] IN (SELECT [ClientId] FROM @DemoClientIds);

                DELETE FROM [dbo].[mechanic_availability]
                WHERE [MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds);

                DELETE FROM [dbo].[mechanics]
                WHERE [MechanicId] IN (SELECT [MechanicId] FROM @DemoMechanicIds);

                DELETE FROM [dbo].[clients]
                WHERE [ClientId] IN (SELECT [ClientId] FROM @DemoClientIds);

                DELETE FROM [dbo].[shops]
                WHERE [ShopId] IN (SELECT [ShopId] FROM @DemoShopIds);

                DELETE FROM [dbo].[audit_logs]
                WHERE [ActorUserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[otp_verifications]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[password_reset_tokens]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[user_device_tokens]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[user_auth_providers]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[user_roles]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);

                DELETE FROM [dbo].[users]
                WHERE [UserId] IN (SELECT [UserId] FROM @DemoUserIds);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
