using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public class VeranstalterModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public string GetOrganizers(string queryString)
        {
            var result = String.Empty;
            using (var reader = databaseManager.ExecuteReader("SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id"))
            {
                result = databaseManager.ReadAsJsonArray(GetOrganizerKeyTableDictionary(), reader);
            }

            var jsonData = Json.SerializeObject(new Dictionary<string, string> { { "veranstalter", result } });
            return Json.QueryJsonData(jsonData, queryString, -1, false, false, Json.ArrayEntryOrKvpValue.ArrayEntry);
        }

        public string GetOrganizer(long id)
        {
            var command = $"SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id WHERE Veranstalter.Id={id}";
            var r = databaseManager.ExecuteReader(command);
            return databaseManager.ReadFirstAsJsonObject(GetOrganizerKeyTableDictionary(), r, null);
        }

        public string GetOrganizerCalendars(long organizerId)
        {
            var reader = databaseManager.ExecuteReader($"SELECT * FROM Kalenderberechtigte WHERE Veranstalter={organizerId}");
            var ids = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Kalender" } }, reader);
            var array = Json.DeserializeArray(ids);
            var idList = new List<long>();
            foreach (var a in array)
            {
                idList.Add(Convert.ToInt64(Json.GetKvpValue(a, "id", false)));
            }
            var result = new KalenderModel().GetCalendars(idList.ToArray());
            return Json.SerializeObject(new Dictionary<string, string> { { "kalender", result } });
        }

        public long PostOrganizer(OrganizerPost organizerPost, out string errorMessage)
        {
            errorMessage = String.Empty;
            var id = new NutzerModel().PostUser(organizerPost.ToUserPost());
            if (id == -1)
            {
                return -1;
            }

            var d = organizerPost.ToDictionary();
            d.Add("Autorisiert", "0");
            d.Add("id", id.ToString());
            
            var result = databaseManager.ExecutePost("Veranstalter", d);

            

            return result;
        }

        public bool PutOrganizer(long id, OrganizerPost organizerPost)
        {
            var user = GetOrganizer(id);
            if (user == null)
            {
                return false;
            }

            return new NutzerModel().PutUser(id, organizerPost.ToUserPost());
        }

        public bool DeleteOrganizer(long id)
        {
            return databaseManager.ExecuteDelete("Veranstalter", id);
        }

        public bool AuthorizeOrganizer(long id)
        {
            var organizer = GetOrganizer(id);
            if (organizer == null)
            {
                return false;
            }

            var user = new NutzerModel().GetUser(id);
            if (Json.GetValue(user, "verifiziert", false) == "0")
            {
                return false;
            }

            return databaseManager.ExecutePut("Veranstalter", id, new Dictionary<string, string> { { "autorisiert", "1" } });
        }

        public Dictionary<string, string> GetOrganizerKeyTableDictionary()
        {
            return new Dictionary<string, string>
            {
                {"id", "Id" },
                {"vorname", "Vorname" },
                {"nachname", "Nachname" },
                {"email", "Email" },
                {"abteilung", "Abteilung" },
                {"verifiziert", "Verifiziert" },
                {"autorisiert", "Autorisiert" },
                //{"passwort", "Passwort" },
            };
        }

        public string GetOrganizerEmail(long id)
        {
            var org = GetOrganizer(id);
            var result = Json.GetValue(org, "email", false);
            return result;
        }

        public bool PostPassword(long id, PasswordPost post, out string errorMessage)
        {
            var auth = new AuthModel();
            errorMessage = String.Empty;
            if (!auth.CheckIfPasswordIsValid(post.neuesPasswort, out errorMessage)){
                return false;
            }

            return databaseManager.ExecutePut("Veranstalter", id, post.ToDictionary());
        }


    }

    public class OrganizerPost : LoginData
    {
        public string passwort { get; set; }
        public string vorname { get; set; }
        public string nachname { get; set; }
        public string email { get; set; }
        public string abteilung { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>
            {
                {"Passwort", Json.SerializeString(HashPassword(passwort)) }
            };

            return result;
        }

        public UserPost ToUserPost()
        {
            return new UserPost { abteilung = abteilung, email = email, vorname = vorname, nachname = nachname };
        }

        public override bool CheckIfPostDataIsValid()
        {
            var validation = new DataValidation();
            return
                validation.CheckIfNameIsValid(passwort);
        }

        
    }

    public class PasswordPost: LoginData
    {
        public string altesPasswort { get; set; }
        public string neuesPasswort { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>
            {
                {"Passwort", Json.SerializeString(HashPassword(neuesPasswort)) }
            };

            return result;
        }

        public override bool CheckIfPostDataIsValid()
        {
            var validation = new DataValidation();
            return
                validation.CheckIfTextIsValid(altesPasswort) &&
                validation.CheckIfTextIsValid(neuesPasswort);
        }
    }
}
