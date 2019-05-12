using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace UsageCalculator
{
    public class EmailSender
    {
        public static void SendEmail(string content, string email, string title)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("phone.statistics@gmail.com");
                message.To.Add(new MailAddress(email));
                message.Subject = title;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = content;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("phone.statistics@gmail.com", "aA12341234");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception) { }
        }
    }
}
