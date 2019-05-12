using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class TimeSample
    {
        public DateTime Time;
        public int Usedcounter;
        public int Unusedcounter;

        public TimeSample(DateTime time)
        {
            Time = time;
            Usedcounter = 0;
            Unusedcounter = 0;
        }

        public TimeSample(DateTime time, int used, int unused)
        {
            Time = time;
            Usedcounter = used;
            Unusedcounter = unused;
        }
    }

    public class SingleSamle
    {
        public string Time;
        public bool WasUsed;
    }
}
