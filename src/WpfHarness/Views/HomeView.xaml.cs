using System.Windows.Input;
using WpfHarness.ViewModels;

namespace WpfHarness.Views;

public partial class HomeView
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;
        if (DataContext is MainViewModel vm && vm.SendCommand.CanExecute(null))
        {
            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }
}
