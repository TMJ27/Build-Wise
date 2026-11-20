using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailSettingsAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ReplyToEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfiguration_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    QuotationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMessage_QuotationRevision_QuotationRevisionId",
                        column: x => x.QuotationRevisionId,
                        principalTable: "QuotationRevision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailMessage_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfiguration_CreatorId",
                table: "EmailConfiguration",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessage_CreatorId",
                table: "EmailMessage",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessage_QuotationRevisionId",
                table: "EmailMessage",
                column: "QuotationRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessage_StatusId_CreationDate",
                table: "EmailMessage",
                columns: new[] { "StatusId", "CreationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfiguration");

            migrationBuilder.DropTable(
                name: "EmailMessage");
        }
    }
}
