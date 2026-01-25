using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace WPFClient;
internal class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new LocalizationManager();

    private ResourceManager _resourceManager;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationManager()
    {
        _resourceManager = new ResourceManager("WPFClient.Localization.Strings", typeof(LocalizationManager).Assembly);
    }

    public void ChangeCulture(string targetCulture)
    {
        CultureInfo newCulture = new(targetCulture);

        Thread.CurrentThread.CurrentUICulture = newCulture;
        Thread.CurrentThread.CurrentCulture = newCulture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));  // null to indicate that everything needs to change to match the new culture.
    }

    public string this[string key] => WPFClient.Localization.Strings.ResourceManager.GetString(key);
}
