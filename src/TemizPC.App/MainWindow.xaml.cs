using System.Windows;
using TemizPC.App.ViewModels;

namespace TemizPC.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
