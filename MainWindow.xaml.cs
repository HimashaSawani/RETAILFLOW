using RetailFlow.ViewModels;
using System.Windows;

namespace RetailFlow;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.InitializeAsync();
            _viewModel.ScannerListener.Attach(this);
        };

        Closed += (_, _) =>
        {
            _viewModel.ScannerListener.Detach(this);
        };
    }
}
