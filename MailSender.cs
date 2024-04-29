using System;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace TestClient
{
    public class MailSender
    {
        public void SendMail(int randomCode, string toEmail, string Name)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("DoctorApp", "shubitidzesaba09@gmail.com"));
            email.To.Add(new MailboxAddress(Name, toEmail));

            email.Subject = "Testing out email sending";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = randomCode.ToString()
            };
            using (var smtp = new SmtpClient())
            {
                smtp.Connect("smtp.server.address", 587, false);

                // Note: only needed if the SMTP server requires authentication
                // smtp.Authenticate("smtp_username", "smtp_password");

                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }
    }
}