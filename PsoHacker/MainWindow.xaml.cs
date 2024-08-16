using System.Windows;
using MahApps.Metro.Controls;

namespace PsoHacker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly MainViewModel _viewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void OnSearchClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.Search();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.Save();
    }
}