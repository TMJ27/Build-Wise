using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatedClientClientTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerTypeId",
                table: "Client");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientTypeId",
                table: "Client",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client",
                column: "ClientTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Client",
                table: "Client",
                column: "ClientTypeId",
                principalTable: "Client",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Client",
                table: "Client");

            migrationBuilder.DropIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ClientTypeId",
                table: "Client");

            migrationBuilder.AddColumn<int>(
                name: "CustomerTypeId",
                table: "Client",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
