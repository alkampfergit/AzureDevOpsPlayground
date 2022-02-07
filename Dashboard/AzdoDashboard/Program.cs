using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using Serilog.Exceptions;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzdoDasboard;

static class Program
{
    static void Main(string[] args)
    {
        ConfigureSerilog();

        // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
        const String collectionUri = "https://dev.azure.com/gianmariaricci";
        const String projectName = "Public";

        var credential = new VssBasicCredential("", "s52ythol7tmierotzhgjix5uvvm6szcmwuecewqnei2luoxvhjca");
        VssConnection connection = new VssConnection(new Uri(collectionUri), credential);
        WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

        // Get 2 levels of query hierarchy items
        List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(projectName, depth: 2).Result;

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
