using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BikeMate.Core.Entities;

namespace BikeMate.WebAdmin.Services;

public interface IAdminOtpEmailService
{
    Task SendLoginOtpAsync(User user, string code, CancellationToken cancellationToken);
}

public sealed class AdminOtpEmailService(
    ILogger<AdminOtpEmailService> logger,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory) : IAdminOtpEmailService
{
    public Task SendLoginOtpAsync(User user, string code, CancellationToken cancellationToken)
    {
        var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? "there" : user.FirstName.Trim();
        const string subject = "Your BikeMate admin login code";
        var plainText = string.Join(
            Environment.NewLine,
            [
                $"Hi {firstName},",
                string.Empty,
                "Use this code to finish signing in to the BikeMate Admin Portal.",
                string.Empty,
                $"Admin login code: {code}",
                string.Empty,
                "This code expires in 10 minutes. If you did not request it, ignore this email and review your admin password.",
                string.Empty,
                "BikeMate"
            ]);

        var html = BuildHtmlEmail(firstName, code);
        return SendAsync(user.Email, subject, plainText, html, cancellationToken, user.Email, code);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string plainText,
        string html,
        CancellationToken cancellationToken,
        string fallbackEmail,
        string fallbackCode)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            if (environment.IsDevelopment())
            {
                logger.LogInformation("Development admin login OTP for {Email}: {OtpCode}", fallbackEmail, fallbackCode);
                return;
            }

            throw new InvalidOperationException("SendGrid is not configured for admin login OTP.");
        }

        var fromEmail = configuration["SendGrid:FromEmail"] ?? configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(fromEmail) || fromEmail.Contains("bikemate.local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SendGrid sender email is not configured for admin login OTP.");
        }

        var fromName = configuration["SendGrid:FromName"] ?? "BikeMate";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = toEmail } }
                }
            },
            from = new { email = fromEmail, name = fromName },
            subject,
            content = new[]
            {
                new { type = "text/plain", value = plainText },
                new { type = "text/html", value = html }
            }
        });

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("SendGrid rejected admin login OTP to {Email}. Status: {Status}. Body: {Body}", toEmail, response.StatusCode, error);
            throw new InvalidOperationException($"Email delivery failed (HTTP {(int)response.StatusCode}). Check the SendGrid API key and sender identity.");
        }
    }

    private static string BuildHtmlEmail(string firstName, string code)
    {
        var body = new StringBuilder();
        body.Append("<div style=\"font-family:Arial,sans-serif;color:#242424;line-height:1.5;max-width:560px\">");
        body.Append("<h2 style=\"color:#ff6b00;margin-bottom:8px\">BikeMate Admin Sign In</h2>");
        body.Append("<p>Hi ").Append(WebUtility.HtmlEncode(firstName)).Append(",</p>");
        body.Append("<p>Use this code to finish signing in to the BikeMate Admin Portal.</p>");
        body.Append("<p style=\"font-size:14px;color:#6e6e6e;margin-bottom:6px\">Your admin login code is</p>");
        body.Append("<div style=\"font-size:30px;font-weight:700;letter-spacing:4px;background:#fff7f2;border:1px solid #ffd1bd;border-radius:8px;padding:14px 18px;display:inline-block;color:#242424\">");
        body.Append(WebUtility.HtmlEncode(code));
        body.Append("</div>");
        body.Append("<ul style=\"padding-left:20px;margin-top:18px\">");
        body.Append("<li>This code expires in 10 minutes.</li>");
        body.Append("<li>If you did not request it, ignore this email and review your admin password.</li>");
        body.Append("<li>BikeMate staff will never ask you to share this code.</li>");
        body.Append("</ul>");
        body.Append("<p style=\"color:#6e6e6e;font-size:13px\">BikeMate</p>");
        body.Append("</div>");
        return body.ToString();
    }
}
