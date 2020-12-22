using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public class TeilnehmerModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public string GetParticipants(string eventUid)
        {
            var id = Convert.ToInt64(databaseManager.GetId($"SELECT * FROM Veranstaltungen WHERE Uid=\"{eventUid}\""));
            return GetParticipants(id);
        }

        public string GetParticipants(long eventId)
        {
            var r = databaseManager.ExecuteReader($"SELECT * FROM Teilnehmer INNER JOIN Nutzerdaten ON Teilnehmer.Nutzer=Nutzerdaten.Id WHERE Veranstaltung = {eventId}");
            return databaseManager.ReadAsJsonArray(GetKeyTableDictionary(), r, "teilnehmer");
        }

        public string GetParticipants()
        {

            var d = GetKeyTableDictionary();
            d.Add("veranstaltung", "Uid");
            var r = databaseManager.ExecuteReader($"SELECT * FROM Teilnehmer INNER JOIN Nutzerdaten ON Teilnehmer.Nutzer=Nutzerdaten.Id INNER JOIN Veranstaltungen ON Teilnehmer.Veranstaltung=Veranstaltungen.Id");
            return databaseManager.ReadAsJsonArray(d, r, "teilnehmer");
        }

        public string GetParticipantsAsArray()
        {
            var r = databaseManager.ExecuteReader($"SELECT * FROM Teilnehmer INNER JOIN Nutzerdaten ON Teilnehmer.Nutzer=Nutzerdaten.Id");
            return databaseManager.ReadAsJsonArray(GetKeyTableDictionary(), r);
        }

        public long AddParticipant(Dictionary<string, string> booking)
        {
            var d = booking;
            var e = Json.GetKvpValue(d, "veranstaltung", false);
            var userId = Json.GetKvpValue(d, "nutzerId", false);

            var result = databaseManager.ExecutePost("Teilnehmer", new Dictionary<string, string>
                {
                    {"Veranstaltung", e },
                    {"Nutzer", userId },
                });

            new VeranstaltungenModel().UpdateParticipantCount(Convert.ToInt64(e));
            return result;
        }

        public bool DeleteParticipant(Dictionary<string, string> booking)
        {
            var d = booking;
            var e = Json.GetKvpValue(d, "veranstaltung", false);
            var userId = Json.GetKvpValue(d, "nutzer", false);
            var id = databaseManager.GetId($"SELECT * FROM Teilnehmer WHERE Veranstaltung={e} AND Id={userId}");
            var result = databaseManager.ExecuteDelete("Teilnehmer", Convert.ToInt64(id));
            new VeranstaltungenModel().UpdateParticipantCount(Convert.ToInt64(e));
            return result;
        }

        public void WipeUnnecessaryData()
        {
            var participants = Json.DeserializeArray(GetParticipantsAsArray());
            var participantEventDictionary = new Dictionary<string, string>();
            var eventIds = new List<string>();
            var outdatedEvents = new List<string>();
            foreach (var participant in participants)
            {
                var o = Json.DeserializeObject(participant);
                var participantId = Json.GetKvpValue(o, "id", false);
                var eventId = Json.GetKvpValue(o, "veranstaltung", false);
                participantEventDictionary.Add(participantId, eventId);
                if (!eventIds.Contains(eventId))
                {
                    eventIds.Add(eventId);
                }
            }

            var v = new VeranstaltungenModel();
            var timeNow = DateTime.Now;

            foreach (var eventId in eventIds)
            {
                var e = v.GetEvent(Convert.ToInt64(eventId));
                if (e == null)
                {
                    outdatedEvents.Add(eventId);
                    continue;
                }

                var date = Json.GetKvpValue(e, "datum", false);
                var deadline = CalendarManager.GetDateTime(date, "2359");
                if (deadline + ConfigManager.GetRetentionPeriodTimeSpan() < timeNow)
                {
                    outdatedEvents.Add(eventId);
                }
            }

            databaseManager.ExecuteDelete("Teilnehmer", "Veranstaltung", outdatedEvents.ToArray());
        }

        public Dictionary<string, string> GetKeyTableDictionary()
        {
            return new Dictionary<string, string>
            {
                {"id", "Id" },
                {"vorname", "Vorname" },
                {"nachname", "Nachname" },
                {"email", "Email" },
                {"abteilung", "Abteilung" },
            };
        }
    }
}
