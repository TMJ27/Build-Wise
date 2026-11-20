using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreationofClientCategorytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "IX_ClientCategory_CreatorId",
                table: "ClientCategory",
                column: "CreatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientCategory");
        }
    }
}
