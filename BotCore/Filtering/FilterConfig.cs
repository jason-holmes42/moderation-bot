using ChatModerationBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Filtering;
internal class FilterConfig : ISettingsConfig
{
    public static string Filename { get; } = "filter.json";
    public List<FilterRule> FilterRules {  get; init; }
    public FilterSettings FilterSettings { get; init; }

    public FilterConfig()
    {

    }
    public FilterConfig(FilterSettings filterSettings, IEnumerable<FilterRule> filterRules)
    {
        FilterSettings = filterSettings;
        FilterRules = filterRules.ToList();
    }
}
