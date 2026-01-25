using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFClient;

public enum MessageResponseState
{
    None,
    Warning,
    Timeout,
    Ban,
    FilterExempt,
    Command
}