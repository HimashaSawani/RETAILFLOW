using RetailFlow.Models;
using RetailFlow.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetailFlow.Views;

public partial class LoginDialog : Window
{
    private readonly AuthService _authService;
    public User? AuthenticatedUser { get; private set; }

    public LoginDialog(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Loaded += (s, e) => PinBox.Focus();
    }

    private void PinDigit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string digit)
        {
            PinBox.Password += digit;
        }
    }

    private void PinClear_Click(object sender, RoutedEventArgs e)
    {
        PinBox.Password = string.Empty;
        ErrorText.Text = string.Empty;
    }

    private async void PinSubmit_Click(object sender, RoutedEventArgs e)
    {
        await AttemptLoginAsync(PinBox.Password);
    }

    private async void PinBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await AttemptLoginAsync(PinBox.Password);
        }
    }

    private async void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        // Auto-submit on 4 digits
        if (PinBox.Password.Length == 4)
        {
            await AttemptLoginAsync(PinBox.Password);
        }
    }

    private async System.Threading.Tasks.Task AttemptLoginAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            ErrorText.Text = "Please enter your 4-digit PIN.";
            return;
        }

        var user = await _authService.AuthenticateByPinAsync(pin.Trim());
        if (user != null)
        {
            AuthenticatedUser = user;
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text = "Invalid PIN code! Please try again.";
            PinBox.Password = string.Empty;
            PinBox.Focus();
        }
    }

    private async void QuickManager_Click(object sender, RoutedEventArgs e)
    {
        await AttemptLoginAsync("1234");
    }

    private async void QuickCashier_Click(object sender, RoutedEventArgs e)
    {
        await AttemptLoginAsync("0000");
    }
}
