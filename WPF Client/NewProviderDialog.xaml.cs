using ChatModerationBot.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFClient;
/// <summary>
/// Interaction logic for NewProviderDialog.xaml
/// </summary>
public partial class NewProviderDialog : Window
{
    public string userIdentity { get; private set; }
    public ProviderID selectedProvider { get; private set; }
    public string? fileToLoad { get; private set; }

    private bool providerIsSet = false;

    public NewProviderDialog()
    {
        InitializeComponent();
    }

    private void Provider_Checked(object sender, RoutedEventArgs e)
    {
        // Since we used x:Static providers:ProviderID to set the tags, they'll come through directly as ProviderIDs, so a direct cast is safe.
        selectedProvider = (ProviderID)((RadioButton)sender).Tag;
        providerIsSet = true;
    }

    private void Options_Checked(object sender, RoutedEventArgs e)
    {
        fileToLoad = ((RadioButton)sender).Tag as string;
    }

    private void btnDialogOK_Click(object sender, RoutedEventArgs e)
    {
        if (!providerIsSet)
        {
            MessageBox.Show(LocalizationManager.Instance["Error_NewProviderSelectProvider"], LocalizationManager.Instance["Error"]);
            return;
        }

        if (selectedProvider == ProviderID.ChatReplay && fileToLoad == null)
        {
            MessageBox.Show(LocalizationManager.Instance["Error_NewProviderSelectDensity"], LocalizationManager.Instance["Error"]);
            return;
        }

        if (txtIdentity.Text == LocalizationManager.Instance["IdentityInputPlaceholder"] || txtIdentity.Text.Contains(' '))
        {
            MessageBox.Show(LocalizationManager.Instance["Error_NewProviderEnterIdentity"], LocalizationManager.Instance["Error"]);
            return;
        }

        userIdentity = txtIdentity.Text;
        DialogResult = true;
    }

    private void Identity_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null && textBox.Text == LocalizationManager.Instance["IdentityInputPlaceholder"])
        {
            textBox.Text = string.Empty;
            textBox.Foreground = new SolidColorBrush(Colors.Black);
        }
    }

    private void Identity_LostFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null && string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Text = LocalizationManager.Instance["IdentityInputPlaceholder"];
            textBox.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }

    private void Identity_Loaded(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        textBox.Text = LocalizationManager.Instance["IdentityInputPlaceholder"];
    }
}
