using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace AisBuchung_Api.Models
{
    public class DatenModel
    {
        public DatabaseManager databaseManager = new DatabaseManager();

        public void WipeUnnecessaryData()
        {
            new BuchungenModel().WipeUnnecessaryData();
            new EmailverifizierungenModel().WipeUnnecessaryData();
            new TeilnehmerModel().WipeUnnecessaryData();
            new NutzerModel().WipeUnnecessaryData();
        }

        public void ClearData()
        {
            CalendarManager.CreateCalendar();
            databaseManager.CreateNewDatabase(true);
        }

        public bool SaveData()
        {
            if (!Directory.Exists("Archiv"))
            {
                Directory.CreateDirectory("Archiv");
            }

            var directory = $"Archiv\\{CalendarManager.GetDateTime(DateTime.Now)}";

            if (Directory.Exists(directory))
            {
                return false;
            }

            Directory.CreateDirectory(directory);
            
            if (File.Exists(CalendarManager.Path))
            {
                File.Copy(CalendarManager.Path, $"{directory}\\{CalendarManager.Path}");
            }

            if (File.Exists(DatabaseManager.Path))
            {
                File.Copy(DatabaseManager.Path, $"{directory}\\{DatabaseManager.Path}");
            }

            if (File.Exists(ConfigManager.Path))
            {
                File.Copy(ConfigManager.Path, $"{directory}\\{ConfigManager.Path}");
            }

            return true;
        }
    }
}
