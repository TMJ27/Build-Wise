using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingcurrencytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Currency_User_UserId",
                table: "Currency");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_Currency_Code",
                table: "Currency");

            migrationBuilder.DropIndex(
                name: "IX_Currency_UserId",
                table: "Currency");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Currency");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Currency",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()")
                .Annotation("Relational:DefaultConstraintName", "DF_Currency_CreationDate")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Currency_CreationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Currency",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(sysutcdatetime())")
                .Annotation("Relational:DefaultConstraintName", "DF_Currency_CreationDate")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Currency_CreationDate");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Currency",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_Currency_Code",
                table: "Currency",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_UserId",
                table: "Currency",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_User_UserId",
                table: "Currency",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
