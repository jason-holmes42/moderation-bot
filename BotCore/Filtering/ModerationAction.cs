using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Filtering;
public class ModerationAction
{
    public string targetUser { get; set; }
    public PunishmentType punishment {  get; set; }
    public TimeSpan? duration { get; set; }            // For custom-length timeouts
    public string reason { get; set; }
}