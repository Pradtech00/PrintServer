using System.IO;
using System.Windows;
using System.Windows.Threading;
using OfficePrintClient.WPF.Models;
using OfficePrintClient.WPF.Services;

namespace OfficePrintClient.WPF;

public partial class MainWindow : Window
{
    private readonly PrintApiClient _api = new();
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        InitializeComponent();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshJobsAsync();
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        var ip = ServerIpBox.Text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            MessageBox.Show("Please enter server IP address.", "Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ConnectBtn.IsEnabled = false;
        StatusText.Text = "Connecting...";

        try
        {
            _api.SetServerAddress(ip);
            var connected = await _api.TestConnectionAsync();

            if (connected)
            {
                StatusText.Text = "Connected!";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                RefreshBtn.IsEnabled = true;
                PrinterCombo.IsEnabled = true;

                await RefreshPrintersAsync();
                await RefreshJobsAsync();
                _refreshTimer.Start();
            }
            else
            {
                StatusText.Text = "Connection failed";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        }
        finally
        {
            ConnectBtn.IsEnabled = true;
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await RefreshPrintersAsync();
    }

    private async Task RefreshPrintersAsync()
    {
        try
        {
            await _api.RefreshPrintersAsync();
            var printers = await _api.GetPrintersAsync();
            PrinterCombo.ItemsSource = printers.Where(p => p.IsActive).ToList();
            PrinterCombo.DisplayMemberPath = "DisplayName";
            StatusBarText.Text = $"Found {printers.Count} printer(s)";
        }
        catch (Exception ex)
        {
            StatusBarText.Text = $"Error refreshing printers: {ex.Message}";
        }
    }

    private async Task RefreshJobsAsync()
    {
        try
        {
            var jobs = await _api.GetJobsAsync();
            JobGrid.ItemsSource = null;
            JobGrid.ItemsSource = jobs;
        }
        catch
        {
            // silently fail on auto-refresh
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files == null || files.Length == 0) return;

        var file = files[0];
        var ext = Path.GetExtension(file).ToLowerInvariant();

        if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".tiff")
        {
            MessageBox.Show("Please drop a PDF or image file.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PrinterCombo.SelectedItem is not PrinterInfo printer)
        {
            MessageBox.Show("Please select a printer first.", "No Printer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DropText.Text = $"Sending {Path.GetFileName(file)}...";
        DropText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

        var result = await _api.SendPrintJobAsync(printer.Id, file);

        if (result != null)
        {
            DropText.Text = $"Job sent! ID: {result.JobId} - Status: {result.Status}";
            DropText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            await RefreshJobsAsync();
        }
        else
        {
            DropText.Text = "Failed to send print job";
            DropText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        }
    }
}
