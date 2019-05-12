using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UsageCalculator;
using Newtonsoft.Json;
using Entities;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

namespace PhoneStatistics.Controllers
{
    [Route("api/[controller]")]
    public class UsageController : Controller
    {
        // GET api/usage
        [HttpGet("currdate")]
        public string Get()
        {
            return JsonConvert.SerializeObject(DateTime.Now);
        }

        // GET api/usage/lessonCode
        [HttpGet("statistics/lessonCode={lessonCode}")]
        public Lesson GetStatistics(string lessonCode)
        {
            Logger.InfoLog($"GetStatistics was called. lessonCode = {lessonCode}");
            Calculator calc = Calculator.GetInstance();
            var lesson = calc.GetLesson(lessonCode);
            Logger.InfoLog($"lesson = {lesson}");
            return lesson;
        }

        // GET api/usage/lessonCode
        [HttpGet("statistics/table/{lessonCode}")]
        public ContentResult ViewLessonByTable(string lessonCode)
        {
            Logger.InfoLog($"ViewLesson was called. lessonCode = {lessonCode}");
            Calculator calc = Calculator.GetInstance();
            var lesson = calc.GetLesson(lessonCode);
            Logger.InfoLog($"lesson = {lesson}");
            var lessonAsHtml = ConvertJsonToHtmlContent(lesson);
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = lessonAsHtml
            };
        }

        // GET api/usage/lessonCode
        [HttpGet("statistics/graph/{lessonCode}")]
        public ContentResult ViewLessonByGraph(string lessonCode)
        {
            Logger.InfoLog($"ViewLesson was called. lessonCode = {lessonCode}");
            Calculator calc = Calculator.GetInstance();
            var lesson = calc.GetLesson(lessonCode);
            Logger.InfoLog($"lesson = {lesson}");
            var lessonAsHtml = ConvertJsonToHtmlContentWithGraph(lesson);

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = lessonAsHtml
            };
        }

        // GET api/usage/allActiveLessons
        [HttpGet("statistics/allActiveLessons")]
        public List<Lesson> AllActiveLessons()
        {
            Logger.InfoLog($"AllActiveLessons was called. lessonCode");
            Calculator calc = Calculator.GetInstance();
            var lessons = calc.AllActiveLessons();
            return lessons;
        }

        // GET api/usage/allOldLessons
        [HttpGet("statistics/allOldLessons")]
        public List<Lesson> AllOldLessons()
        {
            Calculator calc = Calculator.GetInstance();
            var lessons = calc.AllOldLessons();
            return lessons;
        }

        // POST api/usage/startlesson  (Lesson in the body)
        [HttpPost("startlesson")]
        public JsonResult CreateLesson([FromBody] Lesson lesson)
        {
            Logger.InfoLog($"startlesson was called. lesson: {lesson.ClassName}. email: {lesson.TeacherEmail}");
            Calculator calc = Calculator.GetInstance();
            string code = calc.StartLesson(lesson.TeacherEmail, lesson.ClassName);
            Logger.InfoLog($"startlesson. Code: {code}. lesson: {lesson.ClassName}. email: {lesson.TeacherEmail}");
            return Json(code);
        }

        // POST api/usage/closeLesson/lessonCode={lessonCode}
        [HttpPost("closelesson/lessonCode={lessonCode}")]
        public Lesson CloseLesson(string lessonCode)
        {
            Logger.InfoLog($"closeLesson was called. lessonCode: {lessonCode}.");
            Calculator calc = Calculator.GetInstance();
            var lesson = calc.CloseLesson(lessonCode);
            if (lesson != null)
            {
                Logger.InfoLog($"closeLesson: lessonCode: {lessonCode}. email: {lesson.TeacherEmail}.");
                try
                {
                    string mailContent = CreateMailContent(lesson);
                    Thread thread1 = new Thread(() => EmailSender.SendEmail(mailContent, lesson.TeacherEmail, $"Lesson {lesson.ClassName} was ended"));
                    thread1.Start();
                    Thread thread2 = new Thread(() => EmailSender.SendEmail(mailContent, "efratnaus@gmail.com", $"Lesson {lesson.ClassName} was ended"));
                    thread2.Start();
                }
                catch (Exception e)
                {
                    Logger.InfoLog($"failed to send an email: lessonCode: {lessonCode}. error: {e}.");
                }
            }

            Logger.InfoLog($"lesson was closed. lessonCode: {lessonCode}, .lesson: {lesson}");
            return lesson;
        }

        private string CreateMailContent(Lesson lesson)
        {
            string graphUrl = $"http://phonestatistics.azurewebsites.net/api/usage/statistics/graph/{lesson.LessonCode}";
            string tableUrl = $"http://phonestatistics.azurewebsites.net/api/usage/statistics/table/{lesson.LessonCode}";

            return $"<html> <head> </head> <body> " +
                $" <p>Hi,<br>Lesson {lesson.ClassName} has been ended (Lesson code {lesson.LessonCode}). : <br><br></p>" +
                $" To see the lesson's statistics go to: <br> for table view: {tableUrl} <br> For graph view: {graphUrl}" +
                " </body> </html> ";

        }

        private string ConvertJsonToHtmlContent(Lesson lesson)
        {
            // string message = JsonConvert.SerializeObject(lesson);
            StringBuilder message = new StringBuilder($"<html> " +
                "<head> <style> table { font-family: arial, sans-serif; border-collapse: collapse; width: 100%; } td, th { border: 1px solid #dddddd; text-align: left; padding: 8px; }  tr:nth-child(even) { background-color: #dddddd; } </style> </head> " +
                $" <body> <p>Hi,<br>Here are the lesson statistics of {lesson.ClassName}, (Lesson code {lesson.LessonCode}) : <br><br></p>" +
                $" <table> <tr> <th> time </th> <th> Phone Usage (%) </th> </tr> ");

            if (lesson.PhoneUsage.Count > 0)
            {
                DateTime currTime = lesson.PhoneUsage.Keys.Min();
                DateTime lastTime = lesson.PhoneUsage.Keys.Max();
                int usage = 0;
                while (currTime <= lastTime)
                {
                    TimeSample sample;
                    int currUsage;
                    bool found = lesson.PhoneUsage.TryGetValue(currTime, out sample);
                    if (found)
                    {
                        currUsage = 100 * sample.Usedcounter / (sample.Usedcounter + sample.Unusedcounter);
                    }
                    else
                    {
                        currUsage = usage;
                    }

                    message.Append($" <tr> <td> {currTime} </td> <td> {currUsage} (%) </td> </tr> ");

                    usage = currUsage;
                    currTime = currTime.AddMinutes(1);
                }
            }
            
            message.Append(" </table> </body> </html> ");
            return message.ToString();
        }

        private string ConvertJsonToHtmlContentWithGraph(Lesson lesson)
        {
            // string message = JsonConvert.SerializeObject(lesson);
            StringBuilder message = new StringBuilder($"<html> " +
                "<head> <script src=\"https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js\"></script> </head> " +
                $" <body> <p>Hi,<br>Here are the lesson statistics of {lesson.ClassName}, (Lesson code {lesson.LessonCode}) : <br><br></p>" +
                $" <script src=\"http://code.highcharts.com/highcharts.js\"></script> " + 
                "<div id=\"container\" style=\"min-width: 300px; height: 300px; margin: 1em\"></div>");

            string timeStamps = "[]";
            string usages = "[]";

            if (lesson.PhoneUsage.Count > 0)
            {
                DateTime currTime = lesson.PhoneUsage.Keys.Min();
                DateTime lastTime = lesson.PhoneUsage.Keys.Max();
                int usage = 0;

                var allTimeStamps =new StringBuilder("[ ");
                var allUsage = new StringBuilder("[ ");

                while (currTime <= lastTime)
                {
                    TimeSample sample;
                    int currUsage;
                    bool found = lesson.PhoneUsage.TryGetValue(currTime, out sample);
                    if (found)
                    {
                        currUsage = 100 * sample.Usedcounter / (sample.Usedcounter + sample.Unusedcounter);
                    }
                    else
                    {
                        currUsage = usage;
                    }
                    allTimeStamps.Append($"\"{currTime}\" ,");
                    allUsage.Append($"{currUsage}, ");

                    usage = currUsage;
                    currTime = currTime.AddMinutes(1);
                }

                timeStamps = allTimeStamps.ToString();
                timeStamps = timeStamps.Remove(timeStamps.Length - 1) + " ]";

                usages = allUsage.ToString();
                usages = usages.Remove(usages.Length - 1) + " ]";
            }

            message.Append("</body> <script> $('#container').highcharts({" +
                "xAxis: {" + $"categories: {timeStamps} " + "}, " +
                "series: [{ " + $" data: {usages} " + "}] });");

            message.Append(" </script> </html> \n\n");

            return message.ToString();
        }

        // POST api/usage/addsample     AddSample(lessonCode, time, wasUsed)
        [HttpPost("addsample/lessonCode={lessonCode}")]
        public void AddSample(string lessonCode, [FromBody]SingleSamle sample)
        {
            var time = DateTime.Parse(sample.Time);
            var wasUsed = sample.WasUsed;
            Logger.InfoLog($"addsample was called. lessonCode: {lessonCode}. time = {time} . wasUsee = {wasUsed}");
            Calculator calc = Calculator.GetInstance();
            calc.AddSample(lessonCode, time, wasUsed);
        }
    }
}
