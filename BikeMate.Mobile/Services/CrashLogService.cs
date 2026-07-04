using System.Globalization;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

internal static class CrashLogService
{
    private static readonly object SyncRoot = new();
    private static bool installed;
    private static string appName = "BikeMate";

    public static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "crash-logs");

    public static void Install(string applicationName)
    {
        if (installed)
        {
            return;
        }

        installed = true;
        appName = string.IsNullOrWhiteSpace(applicationName) ? appName : applicationName.Trim();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception
                ?? new InvalidOperationException(args.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
            WriteException("Unhandled exception", exception, args.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteException("Unobserved task exception", args.Exception, false);
            args.SetObserved();
        };

#if ANDROID
        try
        {
            var previous = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = new AndroidCrashHandler(previous);
        }
        catch (Exception ex)
        {
            WriteException("Android crash handler install failed", ex, false);
        }
#endif
    }

    public static Task<string> SaveAsync(string title, Exception exception)
    {
        return Task.FromResult(WriteException(title, exception, false));
    }

    private static string WriteException(string title, Exception exception, bool isTerminating)
    {
        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            var fileName = $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.txt";
            var path = Path.Combine(LogDirectory, fileName);
            File.WriteAllText(path, BuildLog(title, exception, isTerminating), Encoding.UTF8);
            return path;
        }
    }

    private static string BuildLog(string title, Exception exception, bool isTerminating)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine($"UTC: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Local: {DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Terminating: {isTerminating}");
        builder.AppendLine($"App: {Safe(() => AppInfo.Current.Name, appName)}");
        builder.AppendLine($"Version: {Safe(() => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})", "unknown")}");
        builder.AppendLine($"Platform: {DeviceInfo.Current.Platform}");
        builder.AppendLine($"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");
        builder.AppendLine($"OS: {DeviceInfo.Current.VersionString}");
        builder.AppendLine();
        AppendException(builder, exception, 0);
        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception, int depth)
    {
        var prefix = depth == 0 ? string.Empty : new string(' ', depth * 2);
        builder.AppendLine($"{prefix}{exception.GetType().FullName}: {exception.Message}");
        builder.AppendLine(exception.StackTrace);

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                builder.AppendLine();
                builder.AppendLine($"{prefix}Aggregate inner:");
                AppendException(builder, inner, depth + 1);
            }
        }
        else if (exception.InnerException is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"{prefix}Inner:");
            AppendException(builder, exception.InnerException, depth + 1);
        }
    }

    private static string Safe(Func<string> read, string fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }

#if ANDROID
    private sealed class AndroidCrashHandler(Java.Lang.Thread.IUncaughtExceptionHandler? previous)
        : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public void UncaughtException(Java.Lang.Thread thread, Java.Lang.Throwable throwable)
        {
            try
            {
                WriteException("Android uncaught exception", new InvalidOperationException(throwable.ToString()), true);
            }
            finally
            {
                if (previous is not null)
                {
                    previous.UncaughtException(thread, throwable);
                }
                else
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
            }
        }
    }
#endif
}
