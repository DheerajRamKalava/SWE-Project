using System.Diagnostics.CodeAnalysis;
using Cloud.Crash;
using Cloud.UI.Views;
using Microsoft.UI.Xaml;

namespace Cloud.UIharness;

/// <summary>
/// Shows the crash dashboard with sample data, or with real Cosmos data when COSMOS_KEY is set.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Creates the window and opens the dashboard.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _ = ShowDashboardAsync();
    }

    private async Task ShowDashboardAsync()
    {
        ICrashReportService service = SampleCrashReportService.Create();

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COSMOS_KEY")))
        {
            try
            {
                CrashPendingUploader pending = new(new LocalCrashStore(), new CosmosCrashUploader());
                service = await CrashReportService.CreateAsync(pending);
            }
            catch (Exception)
            {
                // Fall back to the sample data.
            }
        }

        RootFrame.Navigate(typeof(CrashDashboardPage), new CrashPageContext(service));
    }
}
