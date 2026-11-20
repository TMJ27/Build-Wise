using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuotationCustomerResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuotationResponse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerEmailSnapshot = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationResponse_CustomerAccount_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalTable: "CustomerAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationResponse_QuotationRevision_QuotationRevisionId",
                        column: x => x.QuotationRevisionId,
                        principalTable: "QuotationRevision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationResponse_CustomerAccountId",
                table: "QuotationResponse",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationResponse_QuotationRevisionId",
                table: "QuotationResponse",
                column: "QuotationRevisionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotationResponse");
        }
    }
}
