using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClientClientTypebacttoInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ReplaceEmptyColumn(migrationBuilder, "uniqueidentifier", "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ReplaceEmptyColumn(migrationBuilder, "int", "uniqueidentifier");
        }

        // SQL Server cannot cast GUIDs to integers (or integers to GUIDs).
        // Replacing an empty column is safe; populated records need an explicit
        // business mapping, since ClientType values mean Individual=1/Business=2.
        private static void ReplaceEmptyColumn(MigrationBuilder migrationBuilder,
            string sourceType, string targetType)
        {
            migrationBuilder.Sql($"""
                DECLARE @currentType int = (
                    SELECT system_type_id FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Client') AND name = N'ClientTypeId'
                );

                IF @currentType IS NULL
                    THROW 50002, 'Client.ClientTypeId is missing. Review the database schema before continuing.', 1;

                IF @currentType <> TYPE_ID(N'{targetType}')
                BEGIN
                    IF @currentType <> TYPE_ID(N'{sourceType}')
                        THROW 50002, 'Client.ClientTypeId has an unexpected SQL type. Review the database schema before continuing.', 1;

                    IF EXISTS (SELECT 1 FROM [dbo].[Client] WITH (TABLOCKX, HOLDLOCK))
                        THROW 50002, 'Client.ClientTypeId cannot be converted between GUID and int while Client contains records. Map the existing client types explicitly before retrying. No client records have been changed.', 1;

                    DECLARE @defaultName sysname = (
                        SELECT dc.name FROM sys.default_constraints AS dc
                        INNER JOIN sys.columns AS c
                            ON c.object_id = dc.parent_object_id
                            AND c.column_id = dc.parent_column_id
                        WHERE c.object_id = OBJECT_ID(N'dbo.Client') AND c.name = N'ClientTypeId'
                    );
                    IF @defaultName IS NOT NULL
                    BEGIN
                        DECLARE @dropDefault nvarchar(max) =
                            N'ALTER TABLE [dbo].[Client] DROP CONSTRAINT ' + QUOTENAME(@defaultName);
                        EXEC sys.sp_executesql @dropDefault;
                    END;

                    EXEC sys.sp_executesql N'ALTER TABLE [dbo].[Client] DROP COLUMN [ClientTypeId]';
                    EXEC sys.sp_executesql N'ALTER TABLE [dbo].[Client] ADD [ClientTypeId] {targetType} NOT NULL';
                END;
                """);
        }
    }
}
