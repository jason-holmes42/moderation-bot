using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using ChatModerationBot;

namespace WPFClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindowViewModel mainVM = new();
        MainWindow mainWindow = new(mainVM);

        mainWindow.Show();

        // Initializing the bot must be done async, but we don't really need the associated task.
        _ = mainVM.InitializeAsync();
    }
}
