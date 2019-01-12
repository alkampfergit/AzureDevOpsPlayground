using Serilog;
using Serilog.Exceptions;
using System;

namespace MigrationPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureSerilog();


            Console.WriteLine("Press a key to close!");
            Console.ReadKey();
        }

        private static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    "logs.txt",
                     rollingInterval: RollingInterval.Day
                )
                .WriteTo.File(
                    "errors.txt",
                     rollingInterval: RollingInterval.Day,
                     restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
                )
                .CreateLogger();
        }
    }
}
