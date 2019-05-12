using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Entities
{
    public class Lesson
    {
        public string ClassName;
        public string TeacherEmail;
        public string LessonCode;
        public Dictionary<DateTime, TimeSample> PhoneUsage;

        public Lesson(string className, string teacherEmail)
        {
            ClassName = className;
            TeacherEmail = teacherEmail;
            PhoneUsage = new Dictionary<DateTime, TimeSample>();
            LessonCode = RandomString();
        }

        private static Random random = new Random();
        public static string RandomString()
        {
            int length = 6;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void AddSample(DateTime time, bool wasUsed)
        {
            TimeSample timeSample;
            DateTime currTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
            lock (PhoneUsage)
            {
                bool found = PhoneUsage.TryGetValue(currTime, out timeSample);
                if (!found)
                {
                    timeSample = new TimeSample(currTime);
                    PhoneUsage.Add(currTime, timeSample);
                }

                if (wasUsed)
                {
                    timeSample.Usedcounter = timeSample.Usedcounter + 1;
                } else
                {
                    timeSample.Unusedcounter = timeSample.Unusedcounter + 1;
                }
            }
        }

        public override string ToString()
        {
            return $"{this.LessonCode}, {this.ClassName}, {this.TeacherEmail}";
        }
    }
}
