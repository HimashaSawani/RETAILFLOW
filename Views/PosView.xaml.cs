using RetailFlow.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetailFlow.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                BarcodeScannerBox.Focus();
                Keyboard.Focus(BarcodeScannerBox);
            }, System.Windows.Threading.DispatcherPriority.Input);
        };
    }

    private void BarcodeScannerBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel vm)
            {
                vm.ScanBarcodeCommand.Execute(null);
                Dispatcher.InvokeAsync(() =>
                {
                    BarcodeScannerBox.Focus();
                    Keyboard.Focus(BarcodeScannerBox);
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }

    private void CustomerPhoneBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel vm)
            {
                vm.LookupCustomerCommand.Execute(null);
            }
        }
    }
}
