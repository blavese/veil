using System.Windows;
using Veil.ViewModels;

namespace Veil.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
