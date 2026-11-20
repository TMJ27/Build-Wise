using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeletingClientCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Client",
                table: "Client");

            migrationBuilder.DropTable(
                name: "ClientCategory");

            migrationBuilder.DropIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientCategory_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client",
                column: "ClientTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCategory_CreatorId",
                table: "ClientCategory",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Client",
                table: "Client",
                column: "ClientTypeId",
                principalTable: "ClientCategory",
                principalColumn: "Id");
        }
    }
}
