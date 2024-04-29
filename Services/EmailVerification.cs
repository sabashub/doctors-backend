using System;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

public class EmailVerification
{
    private readonly IConfiguration _configuration;

    public EmailVerification(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Function to generate a random four-digit code
    private static string GenerateVerificationCode()
    {
        Random random = new Random();
        return random.Next(1000, 9999).ToString();
    }

    // Function to send verification code via email using Mailjet API
    private async void SendVerificationEmail(string email, string code)
    {
        try
        {
            var apiKey = _configuration["MailJet:ApiKey"];
            var secretKey = _configuration["MailJet:SecretKey"];

            var client = new MailjetClient(apiKey, secretKey);

            var request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
            .Property(Send.Messages, new JArray {
                new JObject {
                    {"From", new JObject {
                        {"Email", _configuration["Email:From"]},
                        {"Name", _configuration["Email:ApplicationName"]}
                    }},
                    {"To", new JArray {
                        new JObject {
                            {"Email", email}
                        }
                    }},
                    {"Subject", "Verification Code"},
                    {"TextPart", $"Your verification code is: {code}"}
                }
            });

            var response = await client.PostAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Verification code sent to " + email);
            }
            else
            {
                Console.WriteLine("Failed to send verification email: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending email: " + ex.Message);
        }
    }

    public static void Main(string[] args)
    {
        // Load configuration from appsettings.json
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        string email = "user@example.com";
        string code = GenerateVerificationCode();

        var emailVerification = new EmailVerification(configuration);
        emailVerification.SendVerificationEmail(email, code);
    }
}
