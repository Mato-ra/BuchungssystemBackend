using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
using AisBuchung_Api.Models;

namespace AisBuchung_Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            new Models.DatabaseManager().CreateNewDatabase(false);
            Models.ConfigManager.CreateNewConfigFile(false);
            var timer = InitializeTimedMethods();
            var b = CreateHostBuilder(args).Build();
                b.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        public static Timer InitializeTimedMethods()
        {
            var timer = new Timer(86400000 * ConfigManager.GetCleanUpInterval());
            timer.Elapsed += Models.DatenModel.CallWipeUnnecessaryData;
            timer.AutoReset = true;
            timer.Enabled = true;
            return timer;
        }

        
    }
}
