using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Filtering;
internal class FilterConfig
{
    public List<FilterRule> filterRules {  get; set; }
    public FilterSettings filterSettings { get; set; }

    public FilterConfig()
    {

    }
    public FilterConfig(FilterSettings filterSettings, IEnumerable<FilterRule> filterRules)
    {
        this.filterSettings = filterSettings;
        this.filterRules = filterRules.ToList();
    }
}
