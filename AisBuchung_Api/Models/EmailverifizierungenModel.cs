using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;

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
            var r = databaseManager.ReadFirstAsJsonObject(new Dictionary<string, string> { { "nutzer", "Nutzer" } }, reader, null);
            var id = Convert.ToInt64(Json.GetKvpValue(r, "nutzer", false));
            if (new NutzerModel().VerifyUser(id) > 0)
            {
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
