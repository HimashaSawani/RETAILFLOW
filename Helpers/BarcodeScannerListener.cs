using System;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace RetailFlow.Helpers;

public class BarcodeScannerListener
{
    private readonly StringBuilder _buffer = new();
    private readonly Stopwatch _stopwatch = new();
    private const int MaxKeystrokeDelayMs = 70; // Fast scanner wedge delay

    public event Action<string>? BarcodeScanned;

    public void Attach(Window window)
    {
        window.PreviewKeyDown += OnPreviewKeyDown;
    }

    public void Detach(Window window)
    {
        window.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ignore modifiers
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            return;
        }

        long elapsed = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Restart();

        // If delay is too long, reset the buffer (unless empty)
        if (elapsed > MaxKeystrokeDelayMs && _buffer.Length > 0)
        {
            _buffer.Clear();
        }

        if (e.Key == Key.Enter)
        {
            if (_buffer.Length >= 2)
            {
                string barcode = _buffer.ToString().Trim();
                _buffer.Clear();
                PlayBeep();
                BarcodeScanned?.Invoke(barcode);
                e.Handled = true;
            }
            return;
        }

        char? c = KeyToChar(e.Key);
        if (c.HasValue)
        {
            _buffer.Append(c.Value);
        }
    }

    public static void PlayBeep()
    {
        try
        {
            SystemSounds.Beep.Play();
        }
        catch
        {
            // Ignore sound errors
        }
    }

    private static char? KeyToChar(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
            return (char)('0' + (key - Key.D0));

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return (char)('0' + (key - Key.NumPad0));

        if (key >= Key.A && key <= Key.Z)
            return (char)('A' + (key - Key.A));

        if (key == Key.OemMinus || key == Key.Subtract)
            return '-';

        return null;
    }
}
