using System.Windows;
using System.Windows.Controls;
using PsoHacker.ViewModels;

namespace PsoHacker.Views;

public partial class DressingRoomEditor : UserControl
{
    private DressingRoomEditorVm? ViewModel => DataContext as DressingRoomEditorVm;
    
    public DressingRoomEditor()
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