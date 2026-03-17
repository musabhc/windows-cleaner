using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using TemizPC.App.Services;
using TemizPC.App.ViewModels;
using TemizPC.Core.Models;
using TemizPC.Core.Services;

namespace TemizPC.App;

public partial class App : Application
{
    private IAppLogger? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!AdminGuard.IsRunningAsAdministrator())
        {
            MessageBox.Show(
                "TemizPC must be started as administrator.\n\nTemizPC yonetici olarak acilmalidir.",
                "TemizPC",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Shutdown(-1);
            return;
        }

        var environment = AppEnvironment.Current();
        var appVersion = ResolveAppVersion();
        _logger = new JsonFileLogger("TemizPC");

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var localizationService = new LocalizationService();
        var taskDefinitions = CleanupTaskCatalog.CreateDefault(environment);
        var cleanupExecutor = new CleanupExecutor(environment, _logger);
        var releaseSettings = ReleaseSettings.Load(AppContext.BaseDirectory);
        IUpdateService updateService = new VelopackUpdateService(releaseSettings, appVersion, _logger);

        var viewModel = new MainWindowViewModel(
            taskDefinitions,
            cleanupExecutor,
            updateService,
            localizationService,
            _logger,
            appVersion,
            isAdministrator: true);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();

        await viewModel.InitializeAsync();
    }

    private static string ResolveAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+')[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("app.dispatcher_unhandled_exception", e.Exception);
        MessageBox.Show(
            e.Exception.Message,
            "TemizPC",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
        Shutdown(-1);
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.Error("app.unhandled_exception", exception);
        }
    }
}
