using CommunityToolkit.Mvvm.ComponentModel;
using TemizPC.App.Services;
using TemizPC.Core.Models;
using System.Windows.Media;

namespace TemizPC.App.ViewModels;

public partial class CleanupTaskItemViewModel : ObservableObject
{
    private static readonly Brush NeutralBorderBrush = CreateBrush("#24334F");
    private static readonly Brush SelectedBackgroundBrush = CreateBrush("#13233A");
    private static readonly Brush UnselectedBackgroundBrush = CreateBrush("#0E1627");
    private static readonly Brush SafeAccentBrush = CreateBrush("#2DD4BF");
    private static readonly Brush ReviewAccentBrush = CreateBrush("#F59E0B");
    private static readonly Brush AdvancedAccentBrush = CreateBrush("#FB7185");

    private readonly LocalizationService _localizationService;

    public CleanupTaskItemViewModel(CleanupTaskDefinition definition, LocalizationService localizationService)
    {
        Definition = definition;
        _localizationService = localizationService;
        IsSelected = definition.IsDefaultSelected;
    }

    public event EventHandler? SelectionChanged;

    public CleanupTaskDefinition Definition { get; }

    [ObservableProperty]
    private bool isSelected;

    public string Title => _localizationService.Get(Definition.NameResourceKey);

    public string Description => _localizationService.Get(Definition.DescriptionResourceKey);

    public string Warning => Definition.WarningResourceKey is null
        ? string.Empty
        : _localizationService.Get(Definition.WarningResourceKey);

    public bool HasWarning => !string.IsNullOrWhiteSpace(Warning);

    public string RiskLabel => Definition.RiskLevel switch
    {
        CleanupRiskLevel.Safe => _localizationService.Get("Risk_Safe"),
        CleanupRiskLevel.Review => _localizationService.Get("Risk_Review"),
        CleanupRiskLevel.Advanced => _localizationService.Get("Risk_Advanced"),
        _ => _localizationService.Get("Risk_Safe")
    };

    public string PresetLabel => Definition.Preset == CleanupPreset.Recommended
        ? _localizationService.Get("Badge_Recommended")
        : _localizationService.Get("Badge_Advanced");

    public Brush AccentBrush => Definition.RiskLevel switch
    {
        CleanupRiskLevel.Safe => SafeAccentBrush,
        CleanupRiskLevel.Review => ReviewAccentBrush,
        CleanupRiskLevel.Advanced => AdvancedAccentBrush,
        _ => SafeAccentBrush
    };

    public Brush BorderBrush => IsSelected ? AccentBrush : NeutralBorderBrush;

    public Brush BackgroundBrush => IsSelected ? SelectedBackgroundBrush : UnselectedBackgroundBrush;

    public void RefreshText()
    {
        OnPropertyChanged(string.Empty);
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(BackgroundBrush));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
