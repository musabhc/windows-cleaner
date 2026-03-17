using System.Windows;
using Velopack;
using TemizPC.App.Services;

namespace TemizPC.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => { })
            .OnAfterUpdateFastCallback(_ => { })
            .OnBeforeUpdateFastCallback(_ => { })
            .OnBeforeUninstallFastCallback(_ => { })
            .Run();

        if (!AdminGuard.IsRunningAsAdministrator())
        {
            if (AdminGuard.TryRestartAsAdministrator())
            {
                return;
            }

            MessageBox.Show(
                "TemizPC must be started as administrator.\n\nTemizPC yonetici olarak acilmalidir.",
                "TemizPC",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
