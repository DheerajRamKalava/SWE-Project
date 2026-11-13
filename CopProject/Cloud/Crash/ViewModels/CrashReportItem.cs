using System.Globalization;

namespace Cloud.Crash.ViewModels;

/// <summary>
/// A crash report prepared for display in a list.
/// </summary>
public sealed class CrashReportItem
{
    /// <summary>
    /// Creates the item.
    /// </summary>
    /// <param name="report">The report to display.</param>
    public CrashReportItem(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Report = report;
    }

    /// <summary>
    /// Gets the underlying report.
    /// </summary>
    public CrashReport Report { get; }

    /// <summary>
    /// Gets when the crash happened, in local time.
    /// </summary>
    public string TimeText => Report.TimestampUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets a one-line summary for the list.
    /// </summary>
    public string Summary => $"{TimeText}  ·  v{Report.AppVersion}  ·  {Report.UserId ?? "unknown user"}";

    /// <summary>
    /// Gets how the exception was caught, with a fatal marker.
    /// </summary>
    public string SourceText => Report.IsFatal ? Report.Source + " (fatal)" : Report.Source;
}
