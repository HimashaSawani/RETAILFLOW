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

        // Automatically ensure SQLite database and tables are created upon startup
        using (var context = new AppDbContext())
        {
            context.Database.EnsureCreated();
        }
    }
}
