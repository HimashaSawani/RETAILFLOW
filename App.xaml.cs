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

        // Automatically ensure SQLite database and tables are created upon startup
        try
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
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
