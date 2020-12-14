using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;
using System.Net;
using System.Net.Mail;

namespace AisBuchung_Api.Models
{
    public class EmailverifizierungenModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public string GetAllCodes()
        {
            var reader = databaseManager.ExecuteReader("SELECT * FROM Emailverifizierungen");
            return databaseManager.ReadAsJsonArray(GetKeyTableDictionary(), reader, "emailverifizierungen");
        }

        public string AddNewCode(long userId, double days)
        {
            var code = GenerateUniqueCode();
            if (true)
            {
                Console.WriteLine($"<{DateTime.Now}> Neuer e-Mail-Verifizierungscode generiert: {code}");
            }

            var dt = DateTime.Now.AddDays(days);
            var dateTime = CalendarManager.GetDateTime(dt);
            var dict = new Dictionary<string, string> {
                {"Nutzer", userId.ToString() },
                {"Zeitfrist", dateTime },
                {"Code", code },
            };

            if (databaseManager.ExecutePost("Emailverifizierungen", dict) > 0)
            {
                return code;
            }
            else
            {
                return null;
            }
        }

        public long AddNewCodeGetId(long userId, double days)
        {
            var code = GenerateUniqueCode();
            if (true)
            {
                Console.WriteLine($"<{DateTime.Now}> Neuer e-Mail-Verifizierungscode generiert: {code}");
            }

            var dt = DateTime.Now.AddDays(days);
            var dateTime = CalendarManager.GetDateTime(dt);
            var dict = new Dictionary<string, string> {
                {"Nutzer", userId.ToString() },
                {"Zeitfrist", dateTime },
                {"Code", code },
            };

            return databaseManager.ExecutePost("Emailverifizierungen", dict);
        }

        public void WipeUnnecessaryData()
        {
            var dateTime = CalendarManager.GetDateTime(DateTime.Now);
            databaseManager.ExecuteNonQuery($"DELETE FROM Emailverifizierungen WHERE Zeitfrist<={dateTime}");
        }

        public string GenerateUniqueCode()
        {
            string result = null;
            do
            {
                result = Guid.NewGuid().ToString();
            }
            while (databaseManager.CountResults($"SELECT * FROM Emailverifizierungen WHERE Code=\"{result}\"") > 0);

            return result;
        }

        public bool ProcessVerification(string code)
        {
            var dateTime = CalendarManager.GetDateTime(DateTime.Now);
            var reader = databaseManager.ExecuteReader($"SELECT * FROM Emailverifizierungen WHERE Code=@code AND Zeitfrist>={dateTime}", new DatabaseManager.Parameter("@code", Microsoft.Data.Sqlite.SqliteType.Text, code));
            var r = databaseManager.ReadFirstAsJsonObject(new Dictionary<string, string> { { "id", "Id"}, { "nutzer", "Nutzer" } }, reader, null);
            var id = Convert.ToInt64(Json.GetKvpValue(r, "nutzer", false));
            var cid = Convert.ToInt64(Json.GetKvpValue(r, "id", false));
            if (new NutzerModel().VerifyUser(id) > 0)
            {
                //TODO: Emailänderung

                DeleteVerificationCode(GetVerificationCodeId(code));
                return true;
            }
            else
            {
                return false;
            }
        }

        public long GetVerificationCodeId(string code)
        {
            var id = databaseManager.GetId($"SELECT * FROM Emailverifizierungen WHERE Code=\"{code}\"");
            if (id != null)
            {
                return Convert.ToInt64(id);
            }
            else
            {
                return -1;
            }
        }

        public bool DeleteVerificationCode(long id)
        {
            return databaseManager.ExecuteDelete("Emailverifizierungen", id);
        }

        public void SendVerificationMail(string code, string emailAdress)
        {
            var link = ConfigManager.GetVerificationLink() + code;

            var content = $"Bitte verifizieren Sie Ihre e-Mail über diesen Link: {link}";

            //SendEmail("Emailverifizierung", content, emailAdress);
        }

        public void SendEmail(string subject, string content, string emailAddress)
        {
            if (!ConfigManager.CheckIfVerificationMailIsActive())
            {
                return;
            }

            var message = new MailMessage();
            var smtp = new SmtpClient();

            var ml = ConfigManager.GetVerificationMailAdress();
            var pw = ConfigManager.GetVerificationMailPassword();

            message.From = new MailAddress(ml);
            message.To.Add(new MailAddress(emailAddress));
            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = content;

            smtp.Port = ConfigManager.GetVerificationMailPort();
            smtp.Host = ConfigManager.GetVerificationMailHost();
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(ml, pw);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

            smtp.Send(message);
        }

        public Dictionary<string, string> GetKeyTableDictionary()
        {
            return new Dictionary<string, string>
            {
                {"id", "Id" },
                {"code", "Code" },
                {"nutzer", "Nutzer" },
                {"zeitfrist", "Zeitfrist" },
            };
        }
    }
}
