using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClienttoClientCategoryrelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Client",
                table: "Client");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Client",
                table: "Client",
                column: "ClientTypeId",
                principalTable: "ClientCategory",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Client",
                table: "Client");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Client",
                table: "Client",
                column: "ClientTypeId",
                principalTable: "Client",
                principalColumn: "Id");
        }
    }
}
