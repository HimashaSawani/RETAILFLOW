using RetailFlow.Models;
using RetailFlow.Services;
using System.Windows;

namespace RetailFlow.Views;

public partial class QuickCustomerDialog : Window
{
    private readonly CustomerService _customerService;
    public Customer? RegisteredCustomer { get; private set; }

    public QuickCustomerDialog(CustomerService customerService, string initialPhone = "")
    {
        InitializeComponent();
        _customerService = customerService;
        PhoneTextBox.Text = initialPhone;

        Loaded += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(initialPhone))
            {
                PhoneTextBox.Focus();
            }
            else
            {
                NameTextBox.Focus();
            }
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        string phone = PhoneTextBox.Text.Trim();
        string name = NameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(phone))
        {
            ErrorText.Text = "Phone number is required.";
            PhoneTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Customer name is required.";
            NameTextBox.Focus();
            return;
        }

        try
        {
            RegisteredCustomer = await _customerService.RegisterCustomerAsync(phone, name);
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            ErrorText.Text = $"Error: {ex.Message}";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
