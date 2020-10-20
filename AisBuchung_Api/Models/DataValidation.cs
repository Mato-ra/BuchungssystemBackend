using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using JsonSerializer;

namespace AisBuchung_Api.Models
{
    public class DataValidation
    {
        public bool CheckIfTextIsValid(string text)
        {
            if (text == null || text == "null" || text == "\"null\"" || text == "null\"" || text == "\"null")
            {
                return false;
            }

            return true;
        }

        public bool CheckIfEmailAdressIsValid(string email)
        {
            if (!CheckIfTextIsValid(email))
            {
                return false;
            }


            var block = 0;
            for(int i = 0; i < email.Length; i++)
            {
                switch (block)
                {
                    case 0:

                        break;
                    case 1:

                        break;

                    case 2:

                        break;
                }
            }


            return true;
        }

        public bool CheckIfNameIsValid(string name)
        {
            if (!CheckIfTextIsValid(name))
            {
                return false;
            }

            return true;
        }

        public bool CheckIfPasswordIsValid(string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            var requirements = ConfigManager.GetPasswordRequirements();

            if (password == null)
            {
                password = String.Empty;
            }

            var l = Json.GetKvpValue(requirements, "mindestlänge", false);
            var length = Json.DeserializeNumber(l, 4);
            if (length < 4)
            {
                length = 4;
            }

            if (password.Length < length)
            {
                errorMessage = $"Das Passwort benötigt mindestens {length} Zeichen.";
                return false;
            }

            var hasNumber = new Regex(@"[0-9]+");
            if (Json.DeserializeString(Json.GetKvpValue(requirements, "erfordertZiffer", false)).ToLower() == "true" && !hasNumber.IsMatch(password))
            {
                errorMessage = "Das Passwort benötigt mindestens eine Ziffer.";
                return false;
            }

            var hasUpperChar = new Regex(@"[A-Z]+");
            if (Json.DeserializeString(Json.GetKvpValue(requirements, "erfordertGroßbuchstaben", false)).ToLower() == "true" && !hasUpperChar.IsMatch(password))
            {
                errorMessage = "Das Passwort benötigt mindestens einen Großbuchstaben.";
                return false;
            }

            var hasLowerChar = new Regex(@"[a-z]+");
            if (Json.DeserializeString(Json.GetKvpValue(requirements, "erfordertKleinbuchstaben", false)).ToLower() == "true" && !hasLowerChar.IsMatch(password))
            {
                errorMessage = "Das Passwort benötigt mindestens einen Kleinbuchstaben.";
                return false;
            }

            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
            if (Json.DeserializeString(Json.GetKvpValue(requirements, "erfordertSonderzeichen", false)).ToLower() == "true" && !hasSymbols.IsMatch(password))
            {
                errorMessage = "Das Passwort benötigt mindestens ein Sonderzeichen.";
                return false;
            }

            return true;
        }

        public static string ReturnErrorMessage(string errorMessage)
        {
            return Json.AddKeyValuePair("{}", "errorMessage", errorMessage, true);
        }

    }
}
