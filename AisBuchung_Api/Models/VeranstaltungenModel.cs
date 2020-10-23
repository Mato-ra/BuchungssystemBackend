using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public class VeranstaltungenModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public string GetEvents(string queryString)
        {
            var es = CalendarManager.GetEvents();
            return GetEvents(es, queryString);
        }

        public string GetEvents(long calendarId, string queryString)
        {
            var es = CalendarManager.GetEvents(calendarId);
            return GetEvents(es, queryString);
        }

        public string GetEvents(List<string> events, string queryString)
        {
            var es = events;
            var result = new List<string>();
            for (int i = 0; i < es.Count; i++)
            {
                var uid = Json.GetKvpValue(es[i], "uid", false);
                var dbData = databaseManager.ExecuteGet("Veranstaltungen", Json.DeserializeString(uid), GetEventKeyTableDictionary());
                if (dbData != null)
                {
                    result.Add(Json.MergeObjects(new string[] { es[i], dbData }, true));
                }
            }

            var jsonData = Json.SerializeObject(new Dictionary<string, string> { { "veranstaltungen", Json.SerializeArray(result.ToArray()) } });
            return Json.QueryJsonData(jsonData, queryString, -1, false, false, Json.ArrayEntryOrKvpValue.ArrayEntry);
        }

        public string GetEvent(string uid)
        {
            var result = CalendarManager.GetEventAsJsonObject(uid);
            if (result == null)
            {
                return null;
            }

            var command = $"SELECT * FROM Veranstaltungen WHERE Veranstaltungen.Uid=@par1";
            using (var reader = databaseManager.ExecuteReader(command, new DatabaseManager.Parameter[]{ new DatabaseManager.Parameter ("@par1", Microsoft.Data.Sqlite.SqliteType.Text, uid )}))
            {
                result = Json.MergeObjects(new string[] { result, databaseManager.ReadFirstAsJsonObject(GetEventKeyTableDictionary(), reader, null) }, true);
            }

            return result;
        }

        public string GetEvent(long eventId)
        {
            var command = $"SELECT * FROM Veranstaltungen WHERE Veranstaltungen.Id={eventId}";
            using (var reader = databaseManager.ExecuteReader(command))
            {
                return databaseManager.ReadFirstAsJsonObject(GetEventKeyTableDictionary(), reader, null);
            }
        }

        public long GetCalendarId(string uid)
        {
            if (GetEvent(uid) == null)
            {
                return -1;
            }
            else
            {
                return Convert.ToInt64(CalendarManager.GetOrganizerCommonName(uid));
            }
        }

        public string PostEvent(long id, EventPost eventPost)
        {
            eventPost.kalender = id;
            var uid = CalendarManager.AddEvent(eventPost, id.ToString());
            var d = eventPost.ToDictionary();
            d["Uid"] = uid;
            d["Teilnehmerzahl"] = "0";
            var result = databaseManager.ExecutePost("Veranstaltungen", d);
            if (result > 0)
            {
                return uid;
            }
            else
            {
                return null;
            }
        }

        public bool PutEvent(long calendarId, string uid, EventPost eventPost)
        {
            var result = CalendarManager.EditEvent(calendarId, eventPost, uid);
            return result;
        }

        public bool DeleteEvent(long calendarId, string uid)
        {
            var id = calendarId;
            if (CalendarManager.GetEvent(uid).Organizer.CommonName != id.ToString())
            {
                return false;
            }

            databaseManager.ExecuteNonQuery($"DELETE FROM Veranstaltungen WHERE Uid=@uid", new DatabaseManager.Parameter("@uid", Microsoft.Data.Sqlite.SqliteType.Text, uid));
            CalendarManager.DeleteEvent(Convert.ToInt64(id), uid);
            return true;
        }

        public bool UpdateParticipantCount(long eventId)
        {
            var count = databaseManager.CountResults($"SELECT * FROM Teilnehmer WHERE Veranstaltung={eventId}");
            return databaseManager.ExecutePut("Veranstaltungen", eventId, new Dictionary<string, string> { { "Teilnehmerzahl", count.ToString() } });
        }

        public Dictionary<string, string> GetEventKeyTableDictionary()
        {
            return new Dictionary<string, string>
            {
                {"id", "Id" },
                {"uid", "Uid" },
                {"teilnehmerzahl", "Teilnehmerzahl" },
                {"teilnehmerlimit", "Teilnehmerlimit" },
                {"anmeldefrist", "Anmeldefrist" },
                {"öffentlich", "Öffentlich" },
            };
        }


    }

    public class EventPost : LoginData
    {
        public long kalender { get; set; }
        public string name { get; set; }
        public string beschreibung { get; set; }
        public int datum { get; set; }
        public int startzeit { get; set; }
        public int endzeit { get; set; }
        public string ort { get; set; }
        public int teilnehmerlimit { get; set; }
        public int öffentlich { get; set; }
        public long anmeldefrist { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>
            {
                {"Teilnehmerlimit", teilnehmerlimit.ToString() },
            };
            if (anmeldefrist < Convert.ToInt64(CalendarManager.GetDateTime(DateTime.Now)))
            {
                if (new DataValidation().CheckIfIntegerIsValid(startzeit.ToString(), 4, false))
                {
                    anmeldefrist = Convert.ToInt64(datum + startzeit.ToString().PadLeft(4, '0'));
                }
                else
                {
                    anmeldefrist = Convert.ToInt64(datum + "2359");
                }
            }

            result["Anmeldefrist"] = anmeldefrist.ToString();
            if (öffentlich == 1)
            {
                result["Öffentlich"] = "1";
            }
            else
            {
                result["Öffentlich"] = "0";
            }

            return result;
        }

        public override bool CheckIfPostDataIsValid()
        {
            var validation = new DataValidation();
            return 
                validation.CheckIfNameIsValid(name) &&
                validation.CheckIfTextIsValid(beschreibung) &&
                //validation.CheckIfIntegerIsValid(datum, 8, false) &&
                //validation.CheckIfIntegerIsValid(startzeit, 4, false) &&
                //validation.CheckIfIntegerIsValid(endzeit, 4, false) &&
                validation.CheckIfTextIsValid(ort) &&
                //anmeldefrist
                true;
        }
    }
}
