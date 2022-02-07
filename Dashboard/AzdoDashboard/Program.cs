using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Dashboards.WebApi;
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

        // this is the new query copied
        var newQuery = new QueryHierarchyItem()
        {
            Name = "copied",
            Wiql = query.Wiql.Replace("Proximo\\Sprint 1", "Proximo\\Sprint 2"),
            IsFolder = false,
        };

        var newQuerySaved = await witClient.CreateQueryAsync(newQuery, projectName, "Shared Queries/Test");

        // Get dashboard client and get all the dashboard
        var dashClient = connection.GetClient<DashboardHttpClient>();
        var dashboards = await dashClient.GetDashboardsByProjectAsync(new TeamContext(projectName));

        // then find the dashboard with a specific name.
        var dashTest = dashboards.Single(d => d.Name == "DashTests");
        TeamContext teamContext = new TeamContext(projectName, "Proximo Team");
        var dashboard = await dashClient.GetDashboardAsync(
            teamContext,
            dashTest.Id.Value);

        // now iterate in all widgets, replace old query id with the new query id.
        foreach (var widget in dashboard.Widgets)
        {
            //var settings = JsonNode.Parse(widget.Settings);
            //settings["groupKey"] = newQuerySaved.Id;
            //var options = new JsonSerializerOptions { WriteIndented = false };
            //widget.Settings = settings.ToJsonString(options);

            widget.Settings = widget.Settings.Replace(query.Id.ToString(), newQuerySaved.Id.ToString());
            widget.Name += " copied";
        }

        // create the dashboard with the new widget.
        var newDashboard = new Dashboard(dashboard.Widgets);
        newDashboard.Name = "Copied dashboard";
        newDashboard.OwnerId = dashboard.OwnerId;
        await dashClient.CreateDashboardAsync(newDashboard, teamContext);
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
