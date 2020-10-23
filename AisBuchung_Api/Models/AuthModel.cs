using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;
using System.Security.Cryptography;
using System.ComponentModel.Design;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AisBuchung_Api.Models
{
    public class AuthModel : ControllerBase
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public bool CheckIfCalendarPermissions(LoginData loginData, long calendarId)
        {
            if (CheckIfAllPermissions(loginData))
            {
                return true;
            }

            var organizerId = GetLoggedInOrganizer(loginData);
            return CheckIfCalendarPermissions(organizerId, calendarId);
        }

        public bool CheckIfCalendarPermissions(long organizerId, long calendarId)
        {
            if (CheckIfAllPermissions(organizerId))
            {
                return true;
            }

            return CheckIfAdminPermissions(organizerId) || databaseManager.CountResults($"SELECT * FROM Kalenderberechtigte WHERE Kalender={calendarId} AND Veranstalter={organizerId}") == 1;
        }

        public bool CheckIfOrganizerPermissions(long organizerId)
        {
            if (CheckIfAllPermissions(organizerId))
            {
                return true;
            }

            return organizerId > 0;
        }

        public bool CheckIfOrganizerPermissions(LoginData loginData)
        {
            if (CheckIfAllPermissions(loginData))
            {
                return true;
            }

            var organizerId = GetLoggedInOrganizer(loginData);
            return CheckIfOrganizerPermissions(organizerId);
        }

        public bool CheckIfOrganizerPermissions(LoginData loginData, long organizerId)
        {
            if (CheckIfAllPermissions(loginData))
            {
                return true;
            }

            var loginId = GetLoggedInOrganizer(loginData);
            return loginId == organizerId || CheckIfAdminPermissions(loginId);
        }

        public bool CheckIfAdminPermissions(LoginData loginData)
        {
            if (CheckIfAllPermissions(loginData))
            {
                return true;
            }

            var organizerId = GetLoggedInOrganizer(loginData);
            return CheckIfAdminPermissions(organizerId);
        }

        public bool CheckIfAdminPermissions(long organizerId)
        {
            if (CheckIfAllPermissions(organizerId))
            {
                return true;
            }

            return databaseManager.CountResults($"SELECT * FROM Admins WHERE Id={organizerId}") == 1;
        }

        public bool CheckIfAllPermissions(LoginData loginData)
        {
            return CheckIfDebugPermissionsTakePriority() && CheckIfDebugPermissions(loginData);
        }

        public bool CheckIfAllPermissions(long organizerId)
        {
            return CheckIfDebugPermissionsTakePriority() && CheckIfDebugPermissions(organizerId);
        }

        public bool CheckIfDebugPermissionsTakePriority()
        {
            return ConfigManager.CheckIfDebugPermissionGrantAllPower();
        }

        public bool CheckIfDebugPermissions(LoginData loginData)
        {
            return CheckIfDebugPermissions(GetLoggedInOrganizer(loginData));
        }

        public bool CheckIfDebugPermissions(long organizerId)
        {
            return ConfigManager.CheckIfEverybodyHasDebugPermission() || (ConfigManager.CheckIfAdminsHaveDebugPermission() && CheckIfAdminPermissions(organizerId));
        }



        public long GetLoggedInOrganizer(LoginData loginData)
        {
            return GetLoggedInOrganizer(loginData.ml, loginData.HashPassword(loginData.pw));
        }

        public long GetLoggedInOrganizer(string email, string password)
        {
            if (email == null || password == null)
            {
                return -1;
            }

            var result = databaseManager.GetId($"SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id " +
                $"WHERE Email=@email AND Passwort=@password AND Verifiziert=1 AND Autorisiert=1", new DatabaseManager.Parameter[] {
                    new DatabaseManager.Parameter("@email", Microsoft.Data.Sqlite.SqliteType.Text, email),
                    new DatabaseManager.Parameter("@password", Microsoft.Data.Sqlite.SqliteType.Text, password)});

            if (result != null)
            {
                return Convert.ToInt64(result);
            }
            else
            {
                return -1;
            }
        }

        public string GetLoggedInOrganizerData(LoginData loginData)
        {
            var id = GetLoggedInOrganizer(loginData);
            if (id == -1)
            {
                return null;
            }
            else
            {
                return new VeranstalterModel().GetOrganizer(id);
            }
            
        }

        public string GetPermissions(LoginData loginData)
        {
            var id = GetLoggedInOrganizer(loginData);
            var result = new Dictionary<string, string>();
            Json.AddKeyValuePair(result, "veranstalterRechte", Convert.ToInt16(CheckIfOrganizerPermissions(id)).ToString(), true);
            Json.AddKeyValuePair(result, "adminRechte", Convert.ToInt16(CheckIfAdminPermissions(id)).ToString(), true);
            Json.AddKeyValuePair(result, "debugRechte", Convert.ToInt16(CheckIfDebugPermissions(id)).ToString(), true);
            Json.AddKeyValuePair(result, "alleRechte", Convert.ToInt16(CheckIfAllPermissions(id)).ToString(), true);
            return Json.SerializeObject(result);
        }

        public bool CheckIfPasswordIsValid(string password, out string errorMessage)
        {
            return new DataValidation().CheckIfPasswordIsValid(password, out errorMessage);
        }

        public ContentResult CreateErrorMessageResponse(string errorMessage)
        {
            return CreateErrorMessageResponse(errorMessage, 400);
        }

        public ContentResult CreateErrorMessageResponse(string errorMessage, int statusCode)
        {
            var message = DataValidation.ReturnErrorMessage(errorMessage);
            var response = Content(message, "application/json");
            response.StatusCode = statusCode;
            return response;
        }
    }

    public abstract class LoginData
    {
        public string ml { get; set; }
        public string pw { get; set; }
        public abstract bool CheckIfPostDataIsValid();
        public string HashPassword(string password)
        {
            if (password == null)
            {
                return null;
            }

            var sha = SHA512.Create();
            var enc = new System.Text.ASCIIEncoding();
            var result = sha.ComputeHash(enc.GetBytes(password));
            return HashToString(result, false);
        }

        public string HashToString(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for(int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            }

            return result.ToString();
        }
    }

    public class LoginPost : LoginData
    {
        public override bool CheckIfPostDataIsValid()
        {
            return true;
        }
    }
}
