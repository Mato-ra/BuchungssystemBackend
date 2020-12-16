using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public class NutzerModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public string GetUsers(string queryString)
        {
            var result = String.Empty;
            using (var reader = databaseManager.ExecuteReader("SELECT * FROM Nutzerdaten"))
            {
                result = databaseManager.ReadAsJsonArray(GetUserKeyTableDictionary(), reader);
            }

            var jsonData = Json.SerializeObject(new Dictionary<string, string> { { "nutzer", result } });
            return Json.QueryJsonData(jsonData, queryString, -1, false, false, Json.ArrayEntryOrKvpValue.ArrayEntry);
        }

        public string GetUsers(long[] ids)
        {
            return databaseManager.ExecuteGet("Nutzerdaten", ids, GetUserKeyTableDictionary());
        }

        public string GetUser(long id)
        {
            var command = $"SELECT * FROM Nutzerdaten WHERE Id={id}";
            var r = databaseManager.ExecuteReader(command);
            return databaseManager.ReadFirstAsJsonObject(GetUserKeyTableDictionary(), r, null);
        }

        public long PostUser(UserPost userPost)
        {
            var verified = ConfigManager.CheckIfVerificationIsAutomatic();


            if (userPost == null)
            {
                return -1;
            }
            var d = userPost.ToDictionary();
            if (verified)
            {
                d.Add("Verifiziert", "1");
            }
            else
            {
                d.Add("Verifiziert", "0");
            }
            
            var id = databaseManager.ExecutePost("Nutzerdaten", d);
            if (id <= 0)
            {
                return -1;
            }

            if (verified)
            {
                return id;
            }

            var mailModel = new EmailverifizierungenModel();
            var code = mailModel.AddNewCode(id, ConfigManager.GetVerificationTimeInDays());
            if (code != null)
            {
                mailModel.SendVerificationMail(code, userPost.email);
            }
            
            return id;
        }

        public long PostVerifiedUser(UserPost userPost)
        {
            if (userPost == null)
            {
                return -1;
            }
            var d = userPost.ToDictionary();
            d.Add("Verifiziert", "1");

            var id = databaseManager.ExecutePost("Nutzerdaten", d);
            if (id <= 0)
            {
                return -1;
            }

            return id;
        }

        public bool PutUser(long id, UserPost userPost)
        {
            var dict = userPost.ToDictionary();
            dict.Remove("email");
            return databaseManager.ExecutePut("Nutzerdaten", id, dict);
        }

        public long VerifyUser(long id)
        {
            databaseManager.ExecutePut("Nutzerdaten", id, new Dictionary<string, string>{ { "Verifiziert", "1"} });

            var user = databaseManager.ExecuteGet("Nutzerdaten", id, GetUserKeyTableDictionary());
            var u = Json.DeserializeObject(user);
            var email = Json.GetKvpValue(u, "email", false);
            var vorname = Json.GetKvpValue(u, "vorname", false);
            var nachname = Json.GetKvpValue(u, "nachname", false);
            var abteilung = Json.GetKvpValue(u, "abteilung", false);

            var firstId = databaseManager.GetId($"SELECT * FROM Nutzerdaten WHERE Email={email} AND Verifiziert=1");
            if (firstId == null)
            {
                return -1;
            }

            var newId = Convert.ToInt64(firstId);

            if (newId < id)
            {
                var userPost = new UserPost { abteilung = Json.DeserializeString(abteilung), email = Json.DeserializeString(email), vorname = Json.DeserializeString(vorname), nachname = Json.DeserializeString(nachname) };
                if (!PutUser(Convert.ToInt64(firstId), userPost)) { 
                    return -1;
                }

                databaseManager.ExecuteNonQuery($"UPDATE Buchungen SET Nutzer={newId} WHERE Nutzer={id}");
            }

            if (databaseManager.CountResults("SELECT * FROM Veranstalter WHERE Autorisiert=1") == 0 && databaseManager.CountResults($"SELECT * FROM Veranstalter WHERE Id={id}") == 1)
            {
                databaseManager.ExecutePut("Veranstalter", id, new Dictionary<string, string> { { "Autorisiert", "1" } });
            }

            if (databaseManager.CountResults("SELECT * FROM Admins") == 0)
            {
                new AdminsModel().PostAdmin(id);
            }

            return id;
        }


        public bool WipeUnnecessaryData()
        {
            var usedDataList = new List<long>();
            var toBeDeletedIdList = new List<long>();

            var organizers = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, databaseManager.ExecuteReader("SELECT * FROM Veranstalter"));
            foreach(var o in Json.DeserializeArray(organizers))
            {
                var id = Json.GetKvpValue(o, "id", false);
                if (id != null)
                {
                    usedDataList.Add(Convert.ToInt64(id));
                }
            }

            var bookings = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, databaseManager.ExecuteReader("SELECT * FROM Buchungen"));
            foreach (var b in Json.DeserializeArray(bookings))
            {
                var id = Json.GetKvpValue(b, "nutzer", false);
                if (id != null)
                {
                    usedDataList.Add(Convert.ToInt64(id));
                }
            }

            var codes = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, databaseManager.ExecuteReader("SELECT * FROM Emailverifizierungen"));
            foreach (var c in Json.DeserializeArray(codes))
            {
                var id = Json.GetKvpValue(c, "nutzer", false);
                if (id != null)
                {
                    usedDataList.Add(Convert.ToInt64(id));
                }
            }

            var users = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, databaseManager.ExecuteReader("SELECT * FROM Nutzerdaten"));
            foreach (var u in Json.DeserializeArray(users))
            {
                var id = Json.GetKvpValue(u, "id", false);
                if (id != null)
                {
                    var i = Convert.ToInt64(id);
                    if (!usedDataList.Contains(i))
                    {
                        toBeDeletedIdList.Add(i);
                    }
                }
            }

            return databaseManager.ExecuteDelete("Nutzerdaten", toBeDeletedIdList.ToArray());
        }

        public Dictionary<string, string> GetUserKeyTableDictionary()
        {
            return new Dictionary<string, string>
            {
                {"id", "Id" },
                {"vorname", "Vorname" },
                {"nachname", "Nachname" },
                {"email", "Email" },
                {"abteilung", "Abteilung" },
                {"verifiziert", "Verifiziert" },
            };
        }
    }

    public class UserPost : LoginData
    {
        public string vorname { get; set; }
        public string nachname { get; set; }
        public string email { get; set; }
        public string abteilung { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>
            {
                {"Vorname", Json.SerializeString(vorname) },
                {"Nachname", Json.SerializeString(nachname) },
                {"Email", Json.SerializeString(email) },
                {"Abteilung", Json.SerializeString(abteilung) }
            };

            return result;
        }

        public override bool CheckIfPostDataIsValid()
        {
            var validation = new DataValidation();
            return
                validation.CheckIfNameIsValid(vorname) &&
                validation.CheckIfNameIsValid(nachname) &&
                validation.CheckIfEmailAdressIsValid(email) &&
                validation.CheckIfTextIsValid(abteilung);
        }
    }
}
