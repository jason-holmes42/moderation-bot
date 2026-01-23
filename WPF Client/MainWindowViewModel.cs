using ChatModerationBot;
using ChatModerationBot.Core.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFClient;
using WPFClient.Commands;

namespace WPFClient;
public class MainWindowViewModel
{
    BotCore _botCore;

    public ObservableCollection<ProviderView> ProviderViews { get; } = new();
    Dictionary<ProviderID, ProviderView> _currentViews = new();

    public RelayCommand NewProviderCommand => new(execute => StartProvider());

    public async Task InitializeAsync()
    {
        try
        {
            _botCore = await BotCore.CreateAsync("testuser");
        }
        catch (Exception ex)
        {
            // The application only makes sense to exist as a representation of the bot core instance, so it should shut down if there is an error in initialization.
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Failed to initialize bot: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            });
        }
    }

    private async Task StartProvider()
    {
        string userIdentity = "";
        ProviderID selectedProvider = ProviderID.ChatReplay;
        string fileToLoad = "";

        // Open the StartProvider dialog and retrieve results
        NewProviderDialog newProviderDialog = new();
        newProviderDialog.Owner = Application.Current.MainWindow;

        if (newProviderDialog.ShowDialog() == true)
        {
            userIdentity = newProviderDialog.userIdentity;
            selectedProvider = newProviderDialog.selectedProvider;
            fileToLoad = newProviderDialog.fileToLoad!;
        }
        else return;

        // Prevent duplicate provider views for a given platform
        if (_currentViews.ContainsKey(selectedProvider))
        {
            MessageBox.Show($"A {selectedProvider} provider is already running.");
            return;
        }

        // Use NewProvider results to open a new ProviderView for the provider
        ProviderViewModel providerViewModel;

        switch (selectedProvider)
        {
            case ProviderID.ChatReplay:
                // Create the view and its view model
                providerViewModel = await ProviderViewModel.CreateAsync(_botCore, userIdentity, selectedProvider, fileToLoad);

                ProviderView providerView = new(providerViewModel)
                {
                    Width = 800,
                    Height = 450
                };

                Canvas.SetLeft(providerView, 100);
                Canvas.SetTop(providerView, 100);

                providerView.RequestClose += OnProviderCloseRequested;
                ProviderViews.Add(providerView);
                _currentViews[selectedProvider] = providerView;

                // Trigger the view model to begin processing
                await providerViewModel.StartAsync();
                break;
            default:
                // It is theoretically impossible to reach this point without a valid ProviderID, but just in case.
                MessageBox.Show($"{selectedProvider.ToString()} provider not implemented. Please select a different provider.");
                return;
        }
    }

    private void OnProviderCloseRequested(ProviderView providerView)
    {
        ProviderViews.Remove(providerView);
        if (providerView.DataContext is ProviderViewModel viewModel)
        {
            _currentViews.Remove(viewModel.Provider);
        }
    }
}
