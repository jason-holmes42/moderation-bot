using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Filtering;
public class ModerationAction
{
    public string TargetUser { get; set; }
    public PunishmentType Punishment {  get; set; }
    public TimeSpan? Duration { get; set; }            // For custom-length timeouts
    public string Reason { get; set; }
}