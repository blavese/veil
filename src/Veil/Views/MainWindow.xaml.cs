using System.Windows;
using System.Windows.Input;
using Veil.ViewModels;

namespace Veil.Views;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void CopyIp_Click(object sender, MouseButtonEventArgs e)
    {
        if (string.IsNullOrEmpty(Vm.PublicIp) || Vm.PublicIp == "-") return;
        Clipboard.SetText(Vm.PublicIp);
    }
}
