using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Configuration;
internal interface ISettingsConfig
{
    static abstract string Filename { get; }
}
