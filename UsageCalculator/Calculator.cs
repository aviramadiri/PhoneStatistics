using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UsageCalculator
{
    public class Calculator
    {
        public Dictionary<string, Lesson> Lessons;
        public Dictionary<string, Lesson> OldLessons;
        public static Calculator Instance = new Calculator();
        public static object Locker = new object();

        public static Calculator GetInstance()
        {
            /*
            if (Instance == null)
            {
                lock(Locker)
                {
                    if (Instance == null)
                    {
                        Instance = new Calculator();
                    }
                }
            }
            */
            return Instance;
        }

        private Calculator()
        {
            Lessons = new Dictionary<string, Lesson>();
            OldLessons = new Dictionary<string, Lesson>();
        }

        public string StartLesson(string email, string name)
        {
            var lesson = new Lesson(name, email);
            string code = lesson.LessonCode;
            Lessons.Add(code, lesson);
            return code;
        }

        public Lesson CloseLesson(string lessonCode)
        {
            Lesson lesson;
            bool found = false;
            lock (Lessons)
            {
                found = Lessons.TryGetValue(lessonCode, out lesson);
                if (found)
                {
                    // OldLessons.Add(lessonCode, lesson); TODO: should be written to some file or DB
                    Lessons.Remove(lessonCode);
                    Logger.InfoLog($"found lesson!. lessonCode ={lessonCode}");
                }
                else
                {
                    Logger.InfoLog($"not found lesson!. lessonCode ={lessonCode}");
                }
            }

            if (found)
            {
                try
                {
                    WriteLessonToExcel(lesson);
                }
                
                catch (Exception e)
                {
                    Logger.InfoLog($"can't write to a file! LessonCode: {lessonCode}, exception: {e.Message}");
                }
            }

            return lesson;
        }

        private void WriteLessonToExcel(Lesson lesson)
        {
            var usage = lesson.PhoneUsage;
            List<TimeSample> samples = usage.Values.ToList();
            List<TimeSample> sortedSamples = samples.OrderBy(x => x.Time).ToList();
            var code = lesson.LessonCode;
            StringBuilder stringToFile = new StringBuilder($"{lesson.TeacherEmail}#{lesson.ClassName}\n");
            foreach (var sample in sortedSamples)
            {

                stringToFile.Append($"{sample.Time},{sample.Usedcounter},{sample.Unusedcounter}");
                stringToFile.Append("\n");
            }

            System.IO.File.WriteAllText($"{code}.txt", stringToFile.ToString());
        }


        public Lesson ReadFromFile(string code)
        {
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines($"{code}.txt");
            }
            catch (Exception e)
            {
                return null;
            }

            string firstLine = lines[0];
            string mail = firstLine.Split("#")[0];
            string name = firstLine.Split("#")[1];
            Lesson lesson = new Lesson(name, mail);
            lesson.LessonCode = code;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Contains(","))
                {
                    var sample = lines[i].Split(",");
                    lesson.PhoneUsage.Add(DateTime.Parse(sample[0]), new TimeSample(DateTime.Parse(sample[0]), Int32.Parse(sample[1]), Int32.Parse(sample[2])));
                }
            }

            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                Console.WriteLine("\t" + line);
            }

            return lesson;
        }

        public List<Lesson> AllActiveLessons()
        {
            return Lessons.Values.ToList<Lesson>();
        }

        public List<Lesson> AllOldLessons()
        {
            return OldLessons.Values.ToList<Lesson>();
        }

        public Lesson GetLesson(string lessonCode)
        {
            Lesson lesson;
            lock (Lessons)
            {
                try
                {
                    var found = Lessons.TryGetValue(lessonCode, out lesson);
                    if (found)
                    {
                        return lesson;
                    }

                    found = OldLessons.TryGetValue(lessonCode, out lesson);
                    if (found)
                    {
                        return lesson;
                    }
                }
                catch (Exception e)
                {
                    Logger.InfoLog($"can't get lesson! LessonCode: {lessonCode}, exception: {e.Message}");
                }

                try
                {
                    return ReadFromFile(lessonCode);
                }

                catch (Exception e)
                {
                    Logger.InfoLog($"can't read file! LessonCode: {lessonCode}, exception: {e.Message}");
                }
            }

            return null;
        }


        public void AddSample(string lessonCode, DateTime time, bool wasUsed)
        {
            Lesson lesson;
            var found = Lessons.TryGetValue(lessonCode, out lesson);
            if (found)
            {
                lesson.AddSample(time, wasUsed);
            }
        }
    }
}
