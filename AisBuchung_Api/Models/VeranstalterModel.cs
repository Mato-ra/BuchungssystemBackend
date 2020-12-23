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

        public string GetOrganizer(string email)
        {
            var command = $"SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id WHERE Email=@email AND Autorisiert=1 AND Verifiziert=1";
            var r = databaseManager.ExecuteReader(command, new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email));
            return databaseManager.ReadFirstAsJsonObject(GetOrganizerKeyTableDictionary(), r, null);
        }

        public long GetOrganizerId(string email)
        {
            if (email == null)
            {
                return -1;
            }

            var result = databaseManager.GetId($"SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id " +
                $"WHERE Email=@email AND Verifiziert=1 AND Autorisiert=1", new DatabaseManager.Parameter[] {
                    new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email) });

            if (result != null)
            {
                return Convert.ToInt64(result);
            }
            else
            {
                return -1;
            }
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

            if (GetOrganizer(organizerPost.email) != null)
            {
                errorMessage = "Mit dieser e-Mail-Adresse wurde bereits ein Nutzer registriert.";
                return -1;
            }

            var id = new NutzerModel().PostUser(organizerPost.ToUserPost());
            if (id == -1)
            {
                return -1;
            }

            var d = organizerPost.ToDictionary();
            d.Add("Autorisiert", "0");
            d.Add("id", id.ToString());
            var result = databaseManager.ExecutePost("Veranstalter", d);

            if (ConfigManager.CheckIfVerificationIsAutomatic())
            {
                new EmailverifizierungenModel().ProcessVerification(id);
            }


            return result;
        }

        public long PostAuthorizedOrganizer(OrganizerPost organizerPost, out string errorMessage)
        {
            errorMessage = String.Empty;

            if (GetOrganizer(organizerPost.email) != null)
            {
                errorMessage = "Mit dieser e-Mail-Adresse wurde bereits ein Nutzer registriert.";
                return -1;
            }

            var userPost = organizerPost.ToUserPost();

            var id = new NutzerModel().PostUser(userPost);
            if (id == -1)
            {
                return -1;
            }

            var d = organizerPost.ToDictionary();
            d.Add("Autorisiert", "0");
            d.Add("id", id.ToString());
            new EmailverifizierungenModel().ProcessVerification(id);
            var result = databaseManager.ExecutePost("Veranstalter", d);
            if (ForceAuthorizeOrganizer(result))
            {
                return result;
            }

            return -1;
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

            var email = Json.DeserializeString(Json.GetValue(user, "email", false));

            if (databaseManager.ExecutePut("Veranstalter", id, new Dictionary<string, string> { { "autorisiert", "1" } }))
            {
                var ids = databaseManager.ExecuteReader("SELECT a.Id FROM Veranstalter a INNER JOIN Nutzerdaten b ON (a.Id = b.Id) WHERE Email = @email AND Autorisiert = 0", new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email));
                var re = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, ids);
                databaseManager.ExecuteNonQuery($"DELETE FROM Veranstalter WHERE Id IN (SELECT a.Id FROM Veranstalter a INNER JOIN Nutzerdaten b ON (a.Id=b.Id) WHERE Email=@email AND Autorisiert=0)", new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email));
                //TODO: Nichtautorisierte Veranstalter löschen
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ForceAuthorizeOrganizer(long id)
        {
            var organizer = GetOrganizer(id);
            if (organizer == null)
            {
                return false;
            }

            new NutzerModel().VerifyUser(id);

            var user = new NutzerModel().GetUser(id);

            var email = Json.DeserializeString(Json.GetValue(user, "email", false));

            if (databaseManager.ExecutePut("Veranstalter", id, new Dictionary<string, string> { { "autorisiert", "1" } }))
            {
                var ids = databaseManager.ExecuteReader("SELECT a.Id FROM Veranstalter a INNER JOIN Nutzerdaten b ON (a.Id = b.Id) WHERE Email = @email AND Autorisiert = 0", new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email));
                var re = databaseManager.ReadAsJsonArray(new Dictionary<string, string> { { "id", "Id" } }, ids);
                databaseManager.ExecuteNonQuery($"DELETE FROM Veranstalter WHERE Id IN (SELECT a.Id FROM Veranstalter a INNER JOIN Nutzerdaten b ON (a.Id=b.Id) WHERE Email=@email AND Autorisiert=0)", new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email));
                //TODO: Nichtautorisierte Veranstalter löschen
                return true;
            }
            else
            {
                return false;
            }
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

        public bool PostEmail(long id, EmailPost post)
        {
            var veri = new EmailverifizierungenModel();
            var code = veri.AddNewCode(id, ConfigManager.GetVerificationTimeInDays());
            var verId = veri.GetVerificationCodeId(code);
            if (verId < 0)
            {
                return false;
            }
            else
            {
                if (databaseManager.ExecutePost("Emailänderungen", new Dictionary<string, string> { { "Emailverifizierung", verId.ToString() }, { "NeueEmail", post.neueEmail } }) > 0)
                {
                    veri.SendVerificationMail(code, post.neueEmail);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ChangeEmail(long id, string newEmail)
        {
            return databaseManager.ExecutePut("Nutzerdaten", id, new Dictionary<string, string> { { "Email", newEmail } });
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

    public class EmailPost : LoginData
    {
        public string neueEmail { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>
            {
                {"neueEmail", Json.SerializeString(neueEmail) }
            };

            return result;
        }

        public override bool CheckIfPostDataIsValid()
        {
            var validation = new DataValidation();
            return
                validation.CheckIfEmailAdressIsValid(neueEmail);
        }
    }
}
