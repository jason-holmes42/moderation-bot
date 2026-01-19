using ChatModerationBot.Core.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting;

// A fake implementation of ITimeProvider that allows the test to control what time it is.
internal class TestTimeProvider : ITimeProvider
{
    public DateTime CurrentTime { get; set; }

    public DateTime Now => CurrentTime;

    public TestTimeProvider(DateTime targetTime)
    {
        CurrentTime = targetTime;
    }
}
