using Microsoft.EntityFrameworkCore;

namespace NorthwindApp.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NorthwindContext>();

        var sql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'categories' AND column_name = 'discontinued'
                ) THEN
                    ALTER TABLE categories ADD COLUMN discontinued INTEGER NOT NULL DEFAULT 0;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'suppliers' AND column_name = 'discontinued'
                ) THEN
                    ALTER TABLE suppliers ADD COLUMN discontinued INTEGER NOT NULL DEFAULT 0;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'orders' AND column_name = 'discontinued'
                ) THEN
                    ALTER TABLE orders ADD COLUMN discontinued INTEGER NOT NULL DEFAULT 0;
                END IF;
            END
            $$;
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
