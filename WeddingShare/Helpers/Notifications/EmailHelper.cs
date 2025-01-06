using System.Net;
using System.Net.Mail;
using System.Text;

namespace WeddingShare.Helpers.Notifications
{
    public class EmailHelper : INotificationHelper
    {
        private readonly IConfigHelper _config;
        private readonly ISmtpClientWrapper _client;
        private readonly ILogger _logger;

        public EmailHelper(IConfigHelper config, ISmtpClientWrapper client, ILogger<EmailHelper> logger)
        {
            _config = config;
            _client = client;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            if (_config.GetOrDefault("Notifications:Smtp:Enabled", false))
            { 
                try
                {
                    var recipients = _config.GetOrDefault("Notifications:Smtp:Recipient", string.Empty)?.Split(new char[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?.Select(x => new MailAddress(x));
                    if (recipients != null && recipients.Any())
                    { 
                        var host = _config.GetOrDefault("Notifications:Smtp:Host", string.Empty);
                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            var port = _config.GetOrDefault("Notifications:Smtp:Port", 587);
                            if (port > 0)
                            {
                                var from = _config.GetOrDefault("Notifications:Smtp:From", string.Empty);
                                if (!string.IsNullOrWhiteSpace(from))
                                {
                                    var sentToAll = true;
                                    using (var smtp = new SmtpClient(host, port))
                                    {
                                        var username = _config.GetOrDefault("Notifications:Smtp:Username", string.Empty);
                                        var password = _config.GetOrDefault("Notifications:Smtp:Password", string.Empty);
                                        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                                        {
                                            smtp.UseDefaultCredentials = false;
                                            smtp.Credentials = new NetworkCredential(username, password);
                                        }

                                        smtp.EnableSsl = _config.GetOrDefault("Notifications:Smtp:Use_SSL", false);

                                        var sender = new MailAddress(from, _config.GetOrDefault("Notifications:Smtp:DisplayName", "WeddingShare"));
                                        foreach (var to in recipients)
                                        {
                                            try
                                            {
                                                await _client.SendMailAsync(smtp, new MailMessage(new MailAddress(from, _config.GetOrDefault("Notifications:Smtp:DisplayName", "WeddingShare")), to)
                                                {
                                                    Sender = sender,
                                                    Subject = title,
                                                    Body = !string.IsNullOrWhiteSpace(actionLink) ? $"{message}<br/><br/>Visit - {actionLink}" : message,
                                                    IsBodyHtml = true,
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, $"Failed to send email to '{to}' - {ex.Message}");
                                                sentToAll = false;
                                            }
                                        }
                                    }
                
                                    return sentToAll;
                                }
                                else
                                { 
                                    _logger.LogWarning($"Invalid SMTP sender specified");
                                }
                            }
                            else
                            { 
                                _logger.LogWarning($"Invalid SMTP port specified");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid SMTP host specified");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid SMTP recipient specified");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email with title '{title}' - {ex?.Message}");
                }
            }

            return false;
        }
    }
}