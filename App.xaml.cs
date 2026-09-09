using System;
using System.Threading.Tasks;
using System.Windows;
using RetailFlow.Data;

namespace RetailFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global Exception Handling (Phase 17 - Never let the application crash)
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            HandleGlobalException(args.ExceptionObject as Exception, "Critical Domain Error");
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            HandleGlobalException(args.Exception, "Application Error");
            args.Handled = true; // Prevents the application from crashing
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            HandleGlobalException(args.Exception, "Async Task Error");
            args.SetObserved();
        };

        // Automatically ensure SQLite database and tables are created and updated upon startup
        try
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();

            // Ensure newly added columns exist in existing SQLite databases
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN PaymentMethod TEXT DEFAULT 'Cash';"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN AmountTendered NUMERIC DEFAULT 0;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN ChangeDue NUMERIC DEFAULT 0;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN CustomerId INTEGER NULL;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN CustomerPhone TEXT NULL;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN CustomerName TEXT NULL;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN LoyaltyPointsEarned INTEGER DEFAULT 0;"); } catch { }
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database, "ALTER TABLE Sales ADD COLUMN LoyaltyPointsRedeemed INTEGER DEFAULT 0;"); } catch { }

            // Ensure Users table exists
            try
            {
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database,
                    @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        FullName TEXT NOT NULL,
                        Role INTEGER NOT NULL,
                        PinCode TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL
                    );");
            }
            catch { }

            // Ensure Customers table exists
            try
            {
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(context.Database,
                    @"CREATE TABLE IF NOT EXISTS Customers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PhoneNumber TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        LoyaltyPoints INTEGER NOT NULL DEFAULT 0,
                        TotalSpent NUMERIC NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL
                    );");
            }
            catch { }

            // Seed default users if empty
            if (!System.Linq.Queryable.Any(context.Users))
            {
                context.Users.AddRange(
                    new RetailFlow.Models.User { Username = "manager", FullName = "Store Manager", Role = RetailFlow.Models.UserRole.StoreManager, PinCode = "1234" },
                    new RetailFlow.Models.User { Username = "cashier", FullName = "Front Cashier", Role = RetailFlow.Models.UserRole.Cashier, PinCode = "0000" }
                );
                context.SaveChanges();
            }

            // Seed sample loyalty customers if empty
            if (!System.Linq.Queryable.Any(context.Customers))
            {
                context.Customers.AddRange(
                    new RetailFlow.Models.Customer { PhoneNumber = "0771234567", Name = "Nimal Perera", LoyaltyPoints = 250, TotalSpent = 25000 },
                    new RetailFlow.Models.Customer { PhoneNumber = "0719876543", Name = "Sunethra Silva", LoyaltyPoints = 120, TotalSpent = 12000 }
                );
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Database Initialization Error\n\nUnable to initialize the local database: {ex.Message}\nPlease make sure the application has permission to write to its folder.",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void HandleGlobalException(Exception? ex, string title)
    {
        string message = ex?.Message ?? "An unknown error occurred.";
        MessageBox.Show(
            $"❌ Something went wrong.\n\nUnable to complete the operation.\n{message}\n\nPlease try again.",
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
