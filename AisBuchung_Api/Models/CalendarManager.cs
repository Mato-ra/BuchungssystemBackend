using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical;
using Ical.Net.Serialization;
using Ical.Net.DataTypes;
using System.IO;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public static class CalendarManager
    {

        public static string AddEvent(EventPost eventPost, string calendar)
        {
            var c = GetCalendar();
            var result = AddEvent(c, Convert.ToInt64(calendar), eventPost);
            if (result != null)
            {
                SaveCalendar(c);
                return result.Uid;
            }
            else
            {
                return null;
            }
        }

        public static CalendarEvent AddEvent(Calendar calendar, long calendarId, EventPost eventPost)
        {
            var c = calendar;
            var e = new CalendarEvent();
            
            e.Uid = GenerateUniqueId(calendar);

            EditEvent(eventPost.kalender, eventPost, e);

            c.Events.Add(e);
            return e;
        }

        public static string GenerateUniqueId(Calendar calendar)
        {
            while (true)
            {
                var uid = Guid.NewGuid().ToString().Substring(0, ConfigManager.GetUidLength());

                if (CheckIfUidIsUnique(calendar, uid))
                {
                    return uid;
                }
            }
        }

        public static bool EditEvent(long organizerId, EventPost eventPost, string uid)
        {
            var c = GetCalendar();
            var e = GetEvent(uid, c);
            if (e == null)
            {
                return false;
            }

            if (EditEvent(organizerId, eventPost, e))
            {
                SaveCalendar(c);
                return true;
            }

            return false;
        }

        public static bool EditEvent(long calendarId, EventPost eventPost, CalendarEvent calenderEvent)
        {
            var e = calenderEvent;
            var ep = eventPost;

            e.Organizer = new Organizer();
            e.Organizer.CommonName = calendarId.ToString();
            e.Summary = ep.name;
            e.Description = ep.beschreibung;
            e.Location = ep.ort;
            e.DtStart = new CalDateTime(GetDateTime(eventPost.datum.PadLeft(8, '0'), eventPost.startzeit.PadLeft(4, '0')), "Europe/Berlin");
            e.DtEnd = new CalDateTime(GetDateTime(eventPost.datum.PadLeft(8, '0'), eventPost.endzeit.PadLeft(4, '0')), "Europe/Berlin");
            e.DtStamp = new CalDateTime(DateTime.Now, "Europe/Berlin");

            return true;
        }

        public static bool CheckIfUidIsUnique(Calendar calendar, string uid)
        {
            foreach (var e in calendar.Events)
            {
                if (e.Uid == uid)
                {
                    return false;
                }
            }

            return true;
        }

        public static Calendar GetCalendar()
        {
            if (File.Exists(Path))
            {
                return Calendar.Load(File.ReadAllText(Path));
            }
            else
            {
                return CreateCalendar();
            }
        }

        public static string GetOrganizerCommonName(string eventUid)
        {
            var c = GetCalendar();
            var e = c.Events[eventUid];
            var o = e.Organizer;
            return o.CommonName;
        }

        public static Calendar CreateCalendar()
        {
            return new Calendar();
        }

        public static string ReadEventsAsJsonArray()
        {
            return ReadEventsAsJsonArray(GetCalendar());
        }

        public static string ReadEventsAsJsonArray(Calendar calendar)
        {
            if (calendar == null)
            {
                return null;
            }

            return ReadEventsAsJsonArray(calendar.Events.ToList());
        }

        public static string ReadEventsAsJsonArray(List<CalendarEvent> calendarEvents)
        {
            if (calendarEvents == null)
            {
                return null;
            }

            var result = new List<string>();
            foreach (var e in calendarEvents)
            {
                result.Add(ReadEventAsJsonObject(e));
            }

            return Json.SerializeArray(result.ToArray());
        }

        public static string ReadEventsAsJsonArray(Calendar calendar, long calendarId)
        {
            var l = new List<CalendarEvent>();
            var es = calendar.Events;
            foreach(var e in es)
            {
                if (calendarId > -1)
                {
                    if (e.Organizer.CommonName == calendarId.ToString())
                    {
                        l.Add(e);
                    }
                }
            }

            return ReadEventsAsJsonArray(l);
        }

        public static string ReadEventAsJsonObject(Calendar calendar, string uid)
        {
            foreach (var e in calendar.Events)
            {
                if (uid == null || e.Uid == uid)
                {
                    return ReadEventAsJsonObject(e);
                }
            }

            return null;
        }

        public static string ReadEventAsJsonObject(CalendarCollection calendars, string uid)
        {
            foreach(var calendar in calendars)
            {
                var result = ReadEventAsJsonObject(calendar, uid);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static string ReadEventAsJsonObject(CalendarEvent calendarEvent)
        {
            var e = calendarEvent;
            var o = new Dictionary<string, string>();
            Json.AddKeyValuePair(o, "uid", e.Uid, true);
            Json.AddKeyValuePair(o, "name", e.Summary, true);
            Json.AddKeyValuePair(o, "beschreibung", e.Description, true);
            Json.AddKeyValuePair(o, "ort", e.Location, true);
            Json.AddKeyValuePair(o, "datum", GetDate(e.DtStart.Value), true);
            Json.AddKeyValuePair(o, "startzeit", GetDayTime(e.DtStart.Value), true);
            Json.AddKeyValuePair(o, "endzeit", GetDayTime(e.DtEnd.Value), true);
            Json.AddKeyValuePair(o, "jahr", e.DtStart.Year.ToString(), true);
            Json.AddKeyValuePair(o, "monat", e.DtStart.Month.ToString(), true);
            Json.AddKeyValuePair(o, "tag", e.DtStart.Day.ToString(), true);
            return Json.SerializeObject(o);
        }

        public static string GetDayTime(CalDateTime dateTime)
        {
            var result = $"{dateTime.Hour.ToString().PadLeft(2, '0')}{dateTime.Minute.ToString().PadLeft(2, '0')}";
            return Convert.ToInt64(result).ToString();
        }

        public static string GetDate(CalDateTime dateTime)
        {
            var result = $"{dateTime.Year.ToString().PadLeft(4, '0')}{dateTime.Month.ToString().PadLeft(2, '0')}{dateTime.Day.ToString().PadLeft(2, '0')}";
            return Convert.ToInt64(result).ToString();
        }

        public static DateTime GetDateTime(string date, string time)
        {
            var Y = Convert.ToInt32(date.Substring(0, 4));
            var M = Convert.ToInt32(date.Substring(4, 2));
            var D = Convert.ToInt32(date.Substring(6, 2));
            var h = Convert.ToInt32(time.Substring(0, 2));
            var m = Convert.ToInt32(time.Substring(2, 2));
            var result = new DateTime(Y, M, D, h, m, 0);

            return result;
        }

        public static string GetDateTime(DateTime dateTime)
        {
            return $"{dateTime.Year.ToString().PadLeft(4, '0')}{dateTime.Month.ToString().PadLeft(2, '0')}{dateTime.Day.ToString().PadLeft(2, '0')}" +
                $"{dateTime.Hour.ToString().PadLeft(2, '0')}{dateTime.Minute.ToString().PadLeft(2, '0')}";
        }

        public static DateTime GetDateTime(string dateTime)
        {
            return GetDateTime(dateTime.Substring(0, 8), dateTime.Substring(8, 4));
        }

        public static CalendarEvent GetEvent(string uid)
        {
            return GetEvent(uid, GetCalendar());
        }

        public static string GetEventAsJsonObject(string uid)
        {
            return ReadEventAsJsonObject(GetEvent(uid, GetCalendar()));
        }

        public static CalendarEvent GetEvent(string uid, Calendar calendar)
        {
            foreach (var e in calendar.Events)
            {
                if (uid == null || e.Uid == uid)
                {
                    return e;
                }
            }

            return null;
        }

        public static string GetEventsAsJsonArray()
        {
            var result = GetEvents();
            return Json.SerializeArray(result.ToArray());
        }

        public static string GetEventsAsJsonArray(Calendar calendar)
        {
            return Json.SerializeArray(GetEvents(calendar).ToArray());
        }

        public static List<string> GetEvents()
        {
            var c = GetCalendar();
            var result = GetEvents(c);
            return result;
        }

        public static List<string> GetEvents(Calendar calendar)
        {
            var result = new List<string>();
            foreach(var e in calendar.Events)
            {
                result.Add(ReadEventAsJsonObject(e));
            }

            return result;
        }

        public static List<string> GetEvents(long calendarId)
        {
            var c = GetCalendar();
            var result = GetEvents(c, calendarId);
            return result;
        }

        public static List<string> GetEvents(Calendar calendar, long calendarId)
        {
            var result = new List<string>();
            foreach (var e in calendar.Events)
            {
                if (calendarId == -1 || calendarId.ToString() == e.Organizer.CommonName)
                {
                    result.Add(ReadEventAsJsonObject(e));
                }
            }

            return result;
        }

        public static bool DeleteEvent(long calendarId, string uid)
        {
            var c = GetCalendar();
            var es = c.Events;
            for(int i = 0; i < es.Count; i++)
            {
                var e = es[i];
                if (e.Uid == uid && e.Organizer.CommonName == calendarId.ToString())
                {
                    c.Events.Remove(e);
                    return true;
                }
            }

            return false;
        }

        public static bool SaveCalendar(Calendar calendar)
        {
            var cs = new CalendarSerializer(new SerializationContext());
            var result = cs.SerializeToString(calendar);
            File.WriteAllText(Path, result);
            return true;
        }

        public static bool DeleteCalendar(long calendarId)
        {
            return false;
        }

        public const string Path = "kalender.ics";
    }
}
