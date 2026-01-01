using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Filtering;
internal class FilterConfig
{
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
