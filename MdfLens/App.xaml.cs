using System;
using System.IO;
using System.Windows;

namespace MdfLens;

public partial class App : Application
{
    public static string? LoadedFileOnStartup { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try
            {
                File.WriteAllText("crash.log", args.ExceptionObject?.ToString() ?? "Unknown error");
            }
            catch { }
        };

        if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
        {
            var path = e.Args[0].Trim('"');
            if (File.Exists(path))
            {
                LoadedFileOnStartup = path;
            }
        }

        base.OnStartup(e);
    }
}
