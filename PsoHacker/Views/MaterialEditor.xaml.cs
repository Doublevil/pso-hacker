using System.Windows;
using System.Windows.Controls;
using PsoHacker.ViewModels;

namespace PsoHacker.Views;

public partial class MaterialEditor : UserControl
{
    private MaterialEditorVm? ViewModel => DataContext as MaterialEditorVm;
    
    public MaterialEditor()
    {
        InitializeComponent();
    }
    
    private void OnSearchClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.Search();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.Save();
    }
}