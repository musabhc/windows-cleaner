using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TemizPC.App.Services;
using TemizPC.Core.Models;
using TemizPC.Core.Services;
using TemizPC.Core.Utilities;

namespace TemizPC.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICleanupExecutor _cleanupExecutor;
    private readonly IUpdateService _updateService;
    private readonly LocalizationService _localizationService;
    private readonly IAppLogger _logger;
    private readonly Dictionary<CleanupTaskId, CleanupTaskItemViewModel> _taskLookup;

    private CleanupResult? _lastResult;
    private UpdateStatus? _lastUpdateStatus;

    public MainWindowViewModel(
        IEnumerable<CleanupTaskDefinition> taskDefinitions,
        ICleanupExecutor cleanupExecutor,
        IUpdateService updateService,
        LocalizationService localizationService,
        IAppLogger logger,
        string currentVersion,
        bool isAdministrator)
    {
        _cleanupExecutor = cleanupExecutor;
        _updateService = updateService;
        _localizationService = localizationService;
        _logger = logger;
        CurrentVersion = currentVersion;
        IsAdministrator = isAdministrator;

        var taskItems = taskDefinitions
            .Select(definition => new CleanupTaskItemViewModel(definition, _localizationService))
            .ToList();

        foreach (var taskItem in taskItems)
        {
            taskItem.SelectionChanged += OnTaskSelectionChanged;
        }

        _taskLookup = taskItems.ToDictionary(item => item.Definition.Id);
        RecommendedTasks = new ObservableCollection<CleanupTaskItemViewModel>(
            taskItems.Where(item => item.Definition.Preset == CleanupPreset.Recommended));
        AdvancedTasks = new ObservableCollection<CleanupTaskItemViewModel>(
            taskItems.Where(item => item.Definition.Preset == CleanupPreset.Advanced));

        UpdateStatusMessage = _localizationService.Get("Update_NotConfigured");
        ProgressText = _localizationService.Get("Progress_Idle");
        ResultHeadline = _localizationService.Get("Result_Empty");
        ResultSummary = _localizationService.Get("Result_Waiting");
        ResultErrorSummary = _localizationService.Get("Result_NoErrors");

        _localizationService.LanguageChanged += OnLanguageChanged;
        RefreshSelectionState();
    }

    public ObservableCollection<CleanupTaskItemViewModel> RecommendedTasks { get; }

    public ObservableCollection<CleanupTaskItemViewModel> AdvancedTasks { get; }

    public IEnumerable<CleanupTaskItemViewModel> AllTasks => RecommendedTasks.Concat(AdvancedTasks);

    public IReadOnlyList<string> SelectedTaskNames =>
        AllTasks.Where(task => task.IsSelected).Select(task => task.Title).ToList();

    [ObservableProperty]
    private string currentVersion;

    [ObservableProperty]
    private bool isAdministrator;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCheckingForUpdates;

    [ObservableProperty]
    private bool isUpdateAvailable;

    [ObservableProperty]
    private string? availableVersion;

    [ObservableProperty]
    private string updateStatusMessage = string.Empty;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private bool hasResult;

    [ObservableProperty]
    private string resultHeadline = string.Empty;

    [ObservableProperty]
    private string resultSummary = string.Empty;

    [ObservableProperty]
    private string resultErrorSummary = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> lastRunTaskLines = [];

    public string WindowTitle => _localizationService.Get("Window_Title");

    public string HeroTitle => _localizationService.Get("Hero_Title");

    public string HeroSubtitle => _localizationService.Get("Hero_Subtitle");

    public string AdminBadgeText => _localizationService.Get("Admin_Active");

    public string VersionBadgeText => _localizationService.Format("Version_Format", CurrentVersion);

    public string RecommendedPresetTitle => _localizationService.Get("Preset_Recommended_Title");

    public string RecommendedPresetDescription => _localizationService.Get("Preset_Recommended_Description");

    public string AdvancedPresetTitle => _localizationService.Get("Preset_Advanced_Title");

    public string AdvancedPresetDescription => _localizationService.Get("Preset_Advanced_Description");

    public string SelectionTitle => _localizationService.Get("Section_Tasks_Title");

    public string RecommendedSectionTitle => _localizationService.Get("Section_Recommended");

    public string AdvancedSectionTitle => _localizationService.Get("Section_Advanced");

    public string SummaryTitle => _localizationService.Get("Summary_Title");

    public string SummarySelectedCountText => SelectedTaskCount == 0
        ? _localizationService.Get("Summary_NoneSelected")
        : _localizationService.Format("Summary_SelectedCount", SelectedTaskCount);

    public string SummaryRiskText => HighestSelectedRisk switch
    {
        CleanupRiskLevel.Safe => _localizationService.Get("Summary_Risk_Safe"),
        CleanupRiskLevel.Review => _localizationService.Get("Summary_Risk_Review"),
        CleanupRiskLevel.Advanced => _localizationService.Get("Summary_Risk_Advanced"),
        _ => _localizationService.Get("Summary_Risk_Safe")
    };

    public string SummaryGuideText => SelectedTaskCount == 0
        ? _localizationService.Get("Summary_Guide_Default")
        : _localizationService.Get("Summary_Guide_Selected");

    public string SummaryUntouchedText => _localizationService.Get("Summary_Untouched");

    public string RunButtonText => IsBusy
        ? _localizationService.Get("Button_Running")
        : _localizationService.Get("Button_RunCleanup");

    public string OpenLogsButtonText => _localizationService.Get("Button_OpenLogs");

    public string CheckUpdatesButtonText => _localizationService.Get("Button_CheckUpdates");

    public string InstallUpdateButtonText => _localizationService.Get("Button_InstallUpdate");

    public string TurkishButtonText => _localizationService.Get("Language_TR");

    public string EnglishButtonText => _localizationService.Get("Language_EN");

    public bool HasSelectedTasks => SelectedTaskCount > 0;

    public int SelectedTaskCount => AllTasks.Count(task => task.IsSelected);

    public CleanupRiskLevel HighestSelectedRisk => AllTasks
        .Where(task => task.IsSelected)
        .Select(task => task.Definition.RiskLevel)
        .DefaultIfEmpty(CleanupRiskLevel.Safe)
        .Max();

    public bool IsTurkishSelected => _localizationService.IsTurkish;

    public bool IsEnglishSelected => !_localizationService.IsTurkish;

    public async Task InitializeAsync()
    {
        await CheckForUpdatesCoreAsync();
    }

    [RelayCommand]
    private void ApplyRecommendedPreset()
    {
        foreach (var task in AllTasks)
        {
            task.IsSelected = task.Definition.IsDefaultSelected;
        }
    }

    [RelayCommand]
    private void ApplyAdvancedPreset()
    {
        foreach (var task in AllTasks)
        {
            task.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var task in AllTasks)
        {
            task.IsSelected = false;
        }
    }

    private bool CanRunCleanup()
    {
        return !IsBusy && HasSelectedTasks;
    }

    [RelayCommand(CanExecute = nameof(CanRunCleanup))]
    private async Task RunCleanupAsync()
    {
        var selectedTasks = AllTasks
            .Where(task => task.IsSelected)
            .Select(task => task.Definition)
            .ToList();

        if (selectedTasks.Count == 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            HasResult = false;
            ResultHeadline = _localizationService.Get("Result_Working");
            ResultSummary = _localizationService.Get("Result_Running");
            ResultErrorSummary = _localizationService.Get("Result_NoErrors");
            LastRunTaskLines = [];

            var progress = new Progress<CleanupExecutionProgress>(HandleCleanupProgress);
            var result = await _cleanupExecutor.ExecuteAsync(selectedTasks, progress);
            _lastResult = result;
            ApplyResult(result);
        }
        catch (Exception exception)
        {
            _logger.Error("cleanup.run.failed", exception);
            HasResult = true;
            ResultHeadline = _localizationService.Get("Result_Failed");
            ResultSummary = exception.Message;
            ResultErrorSummary = _localizationService.Format("Result_ErrorCount", 1);
            LastRunTaskLines = [];
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 0;
            ProgressText = _localizationService.Get("Progress_Idle");
        }
    }

    private bool CanCheckForUpdates()
    {
        return !IsBusy && !IsCheckingForUpdates;
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        await CheckForUpdatesCoreAsync();
    }

    private bool CanInstallUpdate()
    {
        return !IsBusy && IsUpdateAvailable;
    }

    [RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    private async Task InstallUpdateAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _updateService.DownloadAndApplyAsync();
            UpdateStatusMessage = result.Started
                ? _localizationService.Get("Update_InstallStarted")
                : _localizationService.Format("Update_Error", result.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenLogs()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _logger.LogDirectoryPath,
            UseShellExecute = true,
            Verb = "open"
        };

        Process.Start(processInfo);
    }

    [RelayCommand]
    private void UseTurkish()
    {
        _localizationService.SetCulture("tr-TR");
    }

    [RelayCommand]
    private void UseEnglish()
    {
        _localizationService.SetCulture("en-US");
    }

    partial void OnIsBusyChanged(bool value)
    {
        RunCleanupCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(RunButtonText));
    }

    partial void OnIsCheckingForUpdatesChanged(bool value)
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsUpdateAvailableChanged(bool value)
    {
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    private async Task CheckForUpdatesCoreAsync()
    {
        IsCheckingForUpdates = true;
        try
        {
            _lastUpdateStatus = await _updateService.CheckForUpdatesAsync();
            IsUpdateAvailable = _lastUpdateStatus.IsUpdateAvailable;
            AvailableVersion = _lastUpdateStatus.AvailableVersion;
            UpdateStatusMessage = BuildUpdateMessage(_lastUpdateStatus);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private void HandleCleanupProgress(CleanupExecutionProgress progress)
    {
        ProgressValue = progress.TotalTasks == 0
            ? 0
            : (double)progress.CompletedTasks / progress.TotalTasks;

        if (progress.IsCompleted)
        {
            ProgressText = _localizationService.Get("Progress_Completed");
            return;
        }

        if (progress.CurrentTaskId is CleanupTaskId taskId && _taskLookup.TryGetValue(taskId, out var task))
        {
            ProgressText = _localizationService.Format("Progress_RunningTask", task.Title);
        }
    }

    private void ApplyResult(CleanupResult result)
    {
        HasResult = true;
        ResultHeadline = _localizationService.Get("Result_Completed");
        ResultSummary = _localizationService.Format(
            "Result_Summary",
            ByteSizeFormatter.Format(result.FreedBytes),
            result.DeletedCount,
            result.SkippedCount);

        ResultErrorSummary = result.Errors.Count == 0
            ? _localizationService.Get("Result_NoErrors")
            : _localizationService.Format("Result_ErrorCount", result.Errors.Count);

        LastRunTaskLines = result.TaskResults
            .Select(taskResult => _taskLookup.TryGetValue(taskResult.TaskId, out var task)
                ? _localizationService.Format("Result_TaskLine", task.Title, taskResult.Summary)
                : taskResult.Summary)
            .ToList();
    }

    private string BuildUpdateMessage(UpdateStatus status)
    {
        if (!status.IsConfigured)
        {
            return _localizationService.Get("Update_NotConfigured");
        }

        if (!status.IsInstalled)
        {
            return _localizationService.Get("Update_NotInstalled");
        }

        if (status.IsUpdateAvailable)
        {
            return _localizationService.Format("Update_Available", status.AvailableVersion ?? "?");
        }

        return string.IsNullOrWhiteSpace(status.Message)
            ? _localizationService.Get("Update_UpToDate")
            : _localizationService.Format("Update_Error", status.Message);
    }

    private void OnTaskSelectionChanged(object? sender, EventArgs e)
    {
        RefreshSelectionState();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var task in AllTasks)
        {
            task.RefreshText();
        }

        if (_lastResult is not null)
        {
            ApplyResult(_lastResult);
        }

        if (_lastUpdateStatus is not null)
        {
            UpdateStatusMessage = BuildUpdateMessage(_lastUpdateStatus);
        }

        OnPropertyChanged(string.Empty);
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(SelectedTaskNames));
        OnPropertyChanged(nameof(SelectedTaskCount));
        OnPropertyChanged(nameof(HighestSelectedRisk));
        OnPropertyChanged(nameof(HasSelectedTasks));
        OnPropertyChanged(nameof(SummarySelectedCountText));
        OnPropertyChanged(nameof(SummaryRiskText));
        OnPropertyChanged(nameof(SummaryGuideText));
        OnPropertyChanged(nameof(IsTurkishSelected));
        OnPropertyChanged(nameof(IsEnglishSelected));
        RunCleanupCommand.NotifyCanExecuteChanged();
    }
}
