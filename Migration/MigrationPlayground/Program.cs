using CommandLine;
using MigrationPlayground.Core;
using MigrationPlayground.Core.WorkItems;
using MigrationPlayground.Support;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Collections.Generic;

namespace MigrationPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureSerilog();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var result = CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(opts => options = opts)
               .WithNotParsed<Options>((errs) => HandleParseError(errs));

            if (result.Tag != ParserResultType.Parsed)
            {
                Log.Logger.Error("Command line parameters error, press a key to continue!");
                Console.ReadKey();
                return;
            }

            Connection connection = new Connection(options.ServiceAddress, options.GetAccessToken());

            var importer = new WorkItemImporter(connection, options.OriginalIdField, options.TeamProject);

            //test one item
            MigrationItem mi = new MigrationItem();
            mi.OriginalId = "AA123";
            mi.WorkItemDestinationType = "Product Backlog Item";
            mi.AddVersion(new MigrationItemVersion()
            {
                AuthorEmail = "alkampfer@outlook.com",
                Description = "Description",
                Title = "Title test",
                VersionTimestamp = new DateTime(2010, 01, 23, 22, 10, 32),
            });

            mi.AddVersion(new MigrationItemVersion()
            {
                AuthorEmail = "alkampfer@outlook.com",
                Description = "Description",
                Title = "Title Modified",
                VersionTimestamp = new DateTime(2011, 01, 23, 22, 10, 32),
            });

            mi.AddVersion(new MigrationItemVersion()
            {
                AuthorEmail = "alkampfer@outlook.com",
                Description = "Description",
                Title = "Title Modified Again",
                VersionTimestamp = new DateTime(2012, 01, 23, 22, 10, 32),
            });

            var importResult = importer.ImportWorkItemAsync(mi).Result;
            Log.Information("import result: {importResult}", importResult);

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Execution completed, press a key to continue");
                Console.ReadKey();
            }
        }

        private static void DumpAllTeamProjects(Connection connection)
        {
            foreach (var tpname in connection.GetTeamProjectsNames())
            {
                Log.Debug("Team Project {tpname}", tpname);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(e.ExceptionObject as Exception, "Unhandled exception in the program: {message}", e.ExceptionObject.ToString());
        }

        private static Options options;

        private static void HandleParseError(IEnumerable<Error> errs)
        {

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
