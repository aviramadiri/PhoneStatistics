using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Text;

namespace UsageCalculator
{
    public class Logger
    {
        public static TelemetryClient telemetry = new TelemetryClient();

        public static void InfoLog(string message)
        {
            try
            {
                telemetry.TrackTrace(message);
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
