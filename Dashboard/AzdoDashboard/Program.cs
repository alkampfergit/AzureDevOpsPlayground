using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using Serilog.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace AzdoDasboard;

static class Program
{
    static async Task Main(string[] args)
    {
        ConfigureSerilog();

        // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
        const String collectionUri = "https://dev.azure.com/gianmariaricci";
        const String projectName = "Proximo";

        var pat = ProtectedData.Unprotect(
            File.ReadAllBytes("c:\\secure\\pat.key"),
            new byte[] { 23, 65, 43, 63, 223, 126, 63, 76, 23 },
            DataProtectionScope.CurrentUser);

        var credential = new VssBasicCredential("", Encoding.UTF8.GetString(pat));
        VssConnection connection = new VssConnection(new Uri(collectionUri), credential);
        WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

        // Get and copy query.
        List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(projectName, expand: QueryExpand.All, depth: 2).Result;
        var query = await witClient.GetQueryAsync(projectName, "Shared Queries/Test/TestQuery", expand: QueryExpand.All, depth: 2);

        var newQuery = new QueryHierarchyItem()
        {
            Name = "copied",
            Wiql = query.Wiql.Replace("Proximo\\Sprint 1", "Proximo\\Sprint 2"),
            IsFolder = false,
        };

        await witClient.CreateQueryAsync(newQuery, projectName, "Shared Queries/Test");
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithExceptionDetails()
            .MinimumLevel.Information()
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
