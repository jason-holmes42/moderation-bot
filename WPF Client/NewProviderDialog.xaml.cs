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

    private string identityPlaceholder = "Enter your identity on the chosen platform...";
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
            MessageBox.Show("Please select a provider.");
            return;
        }

        if (selectedProvider == ProviderID.ChatReplay && fileToLoad == null)
        {
            MessageBox.Show("Please select a chat density.");
            return;
        }

        if (txtIdentity.Text == identityPlaceholder || txtIdentity.Text.Contains(' '))
        {
            MessageBox.Show("Please enter an identity for this platform.");
            return;
        }

        userIdentity = txtIdentity.Text;
        DialogResult = true;
    }

    private void Identity_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null && textBox.Text == identityPlaceholder)
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
            textBox.Text = identityPlaceholder;
            textBox.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }
}
