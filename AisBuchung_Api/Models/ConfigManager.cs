using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public static class ConfigManager
    {
        public static void CreateNewConfigFile(bool overwrite)
        {
            if (!File.Exists(Path) || overwrite)
            {
                var configObject = GetDefaultConfiguration();
                var configData = Json.AddFormatting(Json.SerializeObject(configObject));
                File.WriteAllText(Path, configData);
            }
        }

        public static Dictionary<string, string> GetDefaultConfiguration()
        {
            var configObject = new Dictionary<string, string>();
            Json.AddKeyValuePair(configObject, "aufbewahrungsfrist", "14", true);
            Json.AddKeyValuePair(configObject, "datenbereinigungInterval", "0.25", true);
            Json.AddKeyValuePair(configObject, "uidLänge", "8", true);
            Json.AddKeyValuePair(configObject, "emailVerifizierung", Json.SerializeObject(GetVerificationEmailConfigurations()), true);
            Json.AddKeyValuePair(configObject, "debugKonfigurationen", Json.SerializeObject(GetDefaultDebugConfigurations()), true);
            Json.AddKeyValuePair(configObject, "passwortRichtlinien", Json.SerializeObject(GetDefaultPasswordRequirements()), true);
            Json.AddKeyValuePair(configObject, "tokenKonfigurationen", Json.SerializeObject(GetDefaultTokenConfigurations()), true);
            return configObject;
        }

        public static Dictionary<string, string> GetVerificationEmailConfigurations()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "verifizierungsfrist", "0.5", true);
            Json.AddKeyValuePair(result, "verifizierungslink", "frontend.de/verifizieren/", true);
            Json.AddKeyValuePair(result, "emailAdresse", "mail@bbw.de", true);
            Json.AddKeyValuePair(result, "emailPasswort", "r23crm0evfiw1", true);
            Json.AddKeyValuePair(result, "emailHost", "smtp.mail.yahoo.com", true);
            Json.AddKeyValuePair(result, "emailPort", "587", true);
            Json.AddKeyValuePair(result, "automatischeVerifizierung", bool.FalseString.ToLower(), true);
            Json.AddKeyValuePair(result, "adminsKönnenVerifizieren", bool.FalseString.ToLower(), true);
            return result;
        }

        public static Dictionary<string, string> GetDefaultDebugConfigurations()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "alleHabenDebugRechte", bool.FalseString.ToLower(), true);
            Json.AddKeyValuePair(result, "adminsHabenDebugRechte", bool.FalseString.ToLower(), true);
            Json.AddKeyValuePair(result, "debugBerechtigungErlaubtAlles", bool.FalseString.ToLower(), true);
            Json.AddKeyValuePair(result, "debugKonsoleIstAktiv", bool.FalseString.ToLower(), true);
            return result;
        }

        public static Dictionary<string, string> GetDefaultPasswordRequirements()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "erfordertZiffer", bool.TrueString.ToLower(), true);
            Json.AddKeyValuePair(result, "erfordertGroßbuchstaben", bool.TrueString.ToLower(), true);
            Json.AddKeyValuePair(result, "erfordertKleinbuchstaben", bool.TrueString.ToLower(), true);
            Json.AddKeyValuePair(result, "erfordertSonderzeichen", bool.TrueString.ToLower(), true);
            Json.AddKeyValuePair(result, "mindestlänge", "8", true);
            return result;
        }

        public static Dictionary<string, string> GetDefaultTokenConfigurations()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "tokenDauer", "0.5", true);
            Json.AddKeyValuePair(result, "tokenSchlüssel", "YrtHYvdsZ5v74cn5", true);
            return result;
        }

        public static string GetConfigValue(string key)
        {
            var path = "config.json";
            CreateNewConfigFile(false);
            var val = Json.GetValue(File.ReadAllText(path), key, false);
            if (val == null)
            {
                val = Json.GetValue(Json.SerializeObject(GetDefaultConfiguration()), key, false);
            }

            return Json.DeserializeString(val);
        }

        public static string GetConfigValue(string[] key)
        {
            var path = "config.json";
            CreateNewConfigFile(false);
            var data = File.ReadAllText(path);
            var val = Json.GetValue(data, key, false);
            if (val == null)
            {
                val = Json.GetValue(Json.SerializeObject(GetDefaultConfiguration()), key, false);
            }

            return Json.DeserializeString(val);
        }

        public static double GetVerificationTimeInDays()
        {
            return Convert.ToDouble(GetConfigValue(new string[] { "emailVerifizierung", "verifizierungsfrist" }), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static TimeSpan GetRetentionPeriodTimeSpan()
        {
            var days = Convert.ToDouble(GetConfigValue("aufbewahrungsfrist"), System.Globalization.CultureInfo.InvariantCulture);
            return TimeSpan.FromDays(days);
        }

        public static int GetUidLength()
        {
            var result = Convert.ToInt32(GetConfigValue("uidLänge"));
            if (result < 4)
            {
                result = 4;
            }
            if (result > 36)
            {
                result = 36;
            }

            return result;
        }

        public static bool CheckIfVerificationIsAutomatic()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "emailVerifizierung", "automatischeVerifizierung" }));
        }

        public static bool CheckIfAdminsCanVerify()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "emailVerifizierung", "adminsKönnenVerifizieren" }));
        }

        public static bool CheckIfEverybodyHasDebugPermission()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "debugKonfigurationen", "alleHabenDebugRechte" }));
        }

        public static bool CheckIfAdminsHaveDebugPermission()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "debugKonfigurationen", "adminsHabenDebugRechte" }));
        }

        public static bool CheckIfDebugPermissionGrantAllPower()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "debugKonfigurationen", "debugBerechtigungErlaubtAlles" }));
        }

        public static bool CheckIfDebugConsoleIsEnabled()
        {
            return Convert.ToBoolean(GetConfigValue(new string[] { "debugKonfigurationen", "debugKonsoleIstAktiv" }));
        }

        public static double GetCleanUpInterval()
        {
            var result = GetConfigValue("datenbereinigungInterval");
            return Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture);
        }


        public static Dictionary<string, string> GetPasswordRequirements()
        {
            var result = GetConfigValue("passwortRichtlinien");
            if (result == null)
            {
                return GetDefaultPasswordRequirements();
            }
            else
            {
                return Json.DeserializeObject(result);
            }
        }

        public static Dictionary<string, string> GetDebugConfigurations()
        {
            var result = GetConfigValue("debugKonfigurationen");
            if (result == null)
            {
                return GetDefaultPasswordRequirements();
            }
            else
            {
                return Json.DeserializeObject(result);
            }
        }

        public static string GetVerificationMailAdress()
        {
            return GetConfigValue(new string[] { "emailVerifizierung", "emailAdresse" });
        }

        public static string GetVerificationMailPassword()
        {
            return GetConfigValue(new string[] { "emailVerifizierung", "emailPasswort" });
        }

        public static string GetVerificationMailHost()
        {
            return GetConfigValue(new string[] { "emailVerifizierung", "emailHost" });
        }

        public static string GetVerificationLink()
        {
            return GetConfigValue(new string[] { "emailVerifizierung", "verifizierungslink" });
        }

        public static int GetVerificationMailPort()
        {
            return Convert.ToInt32(GetConfigValue(new string[] { "emailVerifizierung", "emailPort" }));
        }

        public static string GetTokenKey()
        {
            return GetConfigValue(new string[] { "tokenKonfigurationen", "tokenSchlüssel" });
        }

        public static double GetTokenExpiry()
        {
            return Convert.ToDouble(GetConfigValue(new string[] { "tokenKonfigurationen", "tokenDauer" }), System.Globalization.CultureInfo.InvariantCulture);
        }

        public const string Path = "config.json";
    }
}
