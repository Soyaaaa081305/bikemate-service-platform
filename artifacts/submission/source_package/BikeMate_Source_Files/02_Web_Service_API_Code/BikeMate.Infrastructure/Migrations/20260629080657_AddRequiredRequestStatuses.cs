using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredRequestStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET IDENTITY_INSERT [dbo].[request_statuses] ON;

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 9)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'emergency_pending' WHERE [StatusId] = 9;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'emergency_pending')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (9, N'emergency_pending');

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 10)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'call_connecting' WHERE [StatusId] = 10;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'call_connecting')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (10, N'call_connecting');

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 11)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'searching_responder' WHERE [StatusId] = 11;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'searching_responder')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (11, N'searching_responder');

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 12)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'payment_pending' WHERE [StatusId] = 12;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'payment_pending')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (12, N'payment_pending');

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 13)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'paid' WHERE [StatusId] = 13;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'paid')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (13, N'paid');

                IF EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusId] = 14)
                    UPDATE [dbo].[request_statuses] SET [StatusName] = N'call_connected' WHERE [StatusId] = 14;
                ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[request_statuses] WHERE [StatusName] = N'call_connected')
                    INSERT INTO [dbo].[request_statuses] ([StatusId], [StatusName]) VALUES (14, N'call_connected');

                SET IDENTITY_INSERT [dbo].[request_statuses] OFF;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
