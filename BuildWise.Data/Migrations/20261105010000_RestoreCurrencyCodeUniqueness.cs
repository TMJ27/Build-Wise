using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations;

public partial class RestoreCurrencyCodeUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing databases may already enforce this constraint. Verify its
        // definition before adopting it rather than creating the same name twice.
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.stats
                WHERE object_id = OBJECT_ID(N'dbo.Currency')
                  AND name = N'UQ_Currency_Code'
            )
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes AS i
                    WHERE i.object_id = OBJECT_ID(N'dbo.Currency')
                      AND i.name = N'UQ_Currency_Code'
                      AND i.is_unique = 1 AND i.is_disabled = 0
                      AND i.has_filter = 0 AND i.is_hypothetical = 0
                      AND (SELECT COUNT(*) FROM sys.index_columns AS ic
                           WHERE ic.object_id = i.object_id
                             AND ic.index_id = i.index_id AND ic.key_ordinal > 0) = 1
                      AND EXISTS (
                          SELECT 1 FROM sys.index_columns AS ic
                          INNER JOIN sys.columns AS c
                              ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                          WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                            AND ic.key_ordinal = 1 AND c.name = N'Code'
                      )
                )
                    THROW 50001, 'UQ_Currency_Code exists but is not an enabled, unfiltered unique index on Currency.Code. Review its definition.', 1;
            END
            ELSE
            BEGIN
                CREATE UNIQUE INDEX [UQ_Currency_Code] ON [dbo].[Currency] ([Code]);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.key_constraints
                WHERE parent_object_id = OBJECT_ID(N'dbo.Currency')
                  AND name = N'UQ_Currency_Code' AND type = 'UQ'
            )
                ALTER TABLE [dbo].[Currency] DROP CONSTRAINT [UQ_Currency_Code];
            ELSE
                DROP INDEX IF EXISTS [UQ_Currency_Code] ON [dbo].[Currency];
            """);
    }
}
