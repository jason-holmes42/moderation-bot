using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPFClient.Commands;
public static class CustomCommands
{
    public static readonly RoutedUICommand Exit = new RoutedUICommand
        (
            LocalizationManager.Instance["ExitCommand"],
            "Exit",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F4, ModifierKeys.Alt)
            }
        );

    public static readonly RoutedUICommand NewProvider = new RoutedUICommand
        (
            LocalizationManager.Instance["NewProviderCommand"],
            "NewProvider",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.N, ModifierKeys.Control)
            }
        );
}
