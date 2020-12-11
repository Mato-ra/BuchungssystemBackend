using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer;
using System.Security.Cryptography;
using System.ComponentModel.Design;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
            var result = GetLoggedInOrganizer(loginData.token);
            if (result != -1)
            {
                return result;
            }

            if (loginData.id > 0)
            {
                result = GetLoggedInOrganizer(loginData.id, loginData.HashPassword(loginData.pw));
                if (result != -1)
                {
                    return result;
                }
            }
            

            return GetLoggedInOrganizer(loginData.ml, loginData.HashPassword(loginData.pw));
        }

        public long GetLoggedInOrganizer(string token)
        {
            return AuthenticateToken(token);
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

        public long GetLoggedInOrganizer(long id, string password)
        {
            if (password == null)
            {
                return -1;
            }

            var result = databaseManager.GetId($"SELECT * FROM Veranstalter INNER JOIN Nutzerdaten ON Veranstalter.Id=Nutzerdaten.Id " +
                $"WHERE Id=@id AND Passwort=@password AND Verifiziert=1 AND Autorisiert=1", new DatabaseManager.Parameter[] {
                    new DatabaseManager.Parameter("@id", Microsoft.Data.Sqlite.SqliteType.Integer, id.ToString()),
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
            var id = GetLoggedInOrganizer(loginData.token);
            if (id != -1)
            {
                return new VeranstalterModel().GetOrganizer(id);
            }

            id = GetLoggedInOrganizer(loginData.ml, loginData.HashPassword(loginData.pw));
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

        public string BuildToken(long id, string password)
        {
            var email = new VeranstalterModel().GetOrganizerEmail(id);

            if (!CheckIfOrganizerPermissions(GetLoggedInOrganizer(email, password)))
            {
                return null;
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigManager.GetTokenKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuer = "http://localhost:5000";
            var audience = "http://localhost:5000";
            var jwtValidity = DateTime.Now.AddDays(ConfigManager.GetTokenExpiry());
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, email, ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(issuer,
              audience,
              claims: claims,
              expires: jwtValidity,
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string BuildToken(string email, string password)
        {
            var id = GetLoggedInOrganizer(email, password);
            if (!CheckIfOrganizerPermissions(id))
            {
                return null;
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigManager.GetTokenKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuer = "http://localhost:5000";
            var audience = "http://localhost:5000";
            var jwtValidity = DateTime.Now.AddDays(ConfigManager.GetTokenExpiry());
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(issuer,
              audience,
              claims: claims,
              expires: jwtValidity,
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private long AuthenticateToken(string token)
        {
            if (token == null)
            {
                return -1;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigManager.GetTokenKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            //List<Exception> validationFailures = null;
            SecurityToken validatedToken;
            var validator = new JwtSecurityTokenHandler();

            // These need to match the values used to generate the token
            TokenValidationParameters validationParameters = new TokenValidationParameters();
            validationParameters.ValidIssuer = "http://localhost:5000";
            validationParameters.ValidAudience = "http://localhost:5000";
            validationParameters.RequireExpirationTime = true;
            validationParameters.IssuerSigningKey = key;
            validationParameters.ValidateIssuerSigningKey = true;
            validationParameters.ValidateAudience = true;
            validationParameters.ValidateLifetime = true;

            if (validator.CanReadToken(token))
            {
                System.Security.Claims.ClaimsPrincipal principal;
                try
                {
                    // This line throws if invalid
                    principal = validator.ValidateToken(token, validationParameters, out validatedToken);
                    

                    // If we got here then the token is valid

                    //TODO: Zu ID wechseln
                    if (principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        var id = Convert.ToInt64(principal.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).First().Value);
                        if (new VeranstalterModel().GetOrganizer(id) != null)
                        {
                            return id;
                        }
                        
                    }
                }
                catch (Exception e)
                {
                    return -1;
                }
            }

            return -1;
        }
    }

    public abstract class LoginData
    {
        public long id { get; set; }
        public string ml { get; set; }
        public string pw { get; set; }
        public string token { get; set; }
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
