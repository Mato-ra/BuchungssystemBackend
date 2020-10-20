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
            Json.AddKeyValuePair(configObject, "verifizierungsfrist", "0.5", true);
            Json.AddKeyValuePair(configObject, "aufbewahrungsfrist", "14", true);
            Json.AddKeyValuePair(configObject, "uidLänge", "8", true);
            /*
            Json.AddKeyValuePair(configObject, "alleHabenDebugRechte", bool.FalseString, true);
            Json.AddKeyValuePair(configObject, "adminsHabenDebugRechte", bool.FalseString, true);
            Json.AddKeyValuePair(configObject, "debugBerechtigungErlaubtAlles", bool.FalseString, true);
            */
            Json.AddKeyValuePair(configObject, "debugKonfigurationen", Json.SerializeObject(GetDefaultDebugConfigurations()), true);
            Json.AddKeyValuePair(configObject, "passwortRichtlinien", Json.SerializeObject(GetDefaultPasswordRequirements()), true);
            return configObject;
        }

        public static Dictionary<string, string> GetDefaultDebugConfigurations()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "alleHabenDebugRechte", bool.FalseString, true);
            Json.AddKeyValuePair(result, "adminsHabenDebugRechte", bool.FalseString, true);
            Json.AddKeyValuePair(result, "debugBerechtigungErlaubtAlles", bool.FalseString, true);
            return result;
        }

        public static Dictionary<string, string> GetDefaultPasswordRequirements()
        {
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "erfordertZiffer", bool.TrueString, true);
            Json.AddKeyValuePair(result, "erfordertGroßbuchstaben", bool.TrueString, true);
            Json.AddKeyValuePair(result, "erfordertKleinbuchstaben", bool.TrueString, true);
            Json.AddKeyValuePair(result, "erfordertSonderzeichen", bool.TrueString, true);
            Json.AddKeyValuePair(result, "mindestlänge", "8", true);
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
            var val = Json.GetValue(File.ReadAllText(path), key, false);
            if (val == null)
            {
                val = Json.GetValue(Json.SerializeObject(GetDefaultConfiguration()), key, false);
            }

            return Json.DeserializeString(val);
        }

        public static double GetVerificationTimeInDays()
        {
            return Convert.ToDouble(GetConfigValue("verifizierungsfrist"));
        }

        public static TimeSpan GetRetentionPeriodTimeSpan()
        {
            var days = Convert.ToDouble(GetConfigValue("aufbewahrungsfrist"));
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

        public const string Path = "config.json";
    }
}
