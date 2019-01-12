using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationPlayground.Core.WorkItems
{
    public class WorkItemImporter
    {
        private readonly Connection connection;
        private readonly string fieldWithOriginalId;
        private readonly string teamProjectName;
        private readonly Project teamProject;

        public WorkItemImporter(
            Connection connection,
            String fieldWithOriginalId,
            String teamProjectName)
        {
            this.connection = connection;
            this.fieldWithOriginalId = fieldWithOriginalId;
            this.teamProjectName = teamProjectName;
            teamProject = connection.GetTeamProject(teamProjectName);
        }

        public async Task<Boolean> ImportWorkItemAsync(MigrationItem itemToMigrate)
        {
            var existingWorkItem = GetWorkItem(itemToMigrate.OriginalId);
            if (existingWorkItem != null)
            {
                Log.Information("A workitem with originalId {originalId} already exists, it will be deleted", itemToMigrate.OriginalId);
                connection.WorkItemStore.DestroyWorkItems(new[] { existingWorkItem.Id });
            }

            WorkItem workItem = CreateWorkItem(itemToMigrate);
            if (workItem == null)
            {
                return false;
            }

            //now that we have work item, we need to start creating all the versions
            for (int i = 0; i < itemToMigrate.Versions.Count(); i++)
            {
                var version = itemToMigrate.GetVersionAt(i);
                workItem.Fields["System.ChangedDate"].Value = version.VersionTimestamp;
                workItem.Fields["System.ChangedBy"].Value = version.AuthorEmail;
                if (i == 0)
                {
                    workItem.Fields["System.CreatedBy"].Value = version.AuthorEmail;
                }
                workItem.Title = version.Title;
                var validation = workItem.Validate();
                if (validation.Count > 0)
                {
                    Log.Error("N°{errCount} validation errors for work Item {workItemId} originalId {originalId}", validation.Count, workItem.Id, itemToMigrate.OriginalId);
                    foreach (Field error in validation)
                    {
                        Log.Error("Version {version}: We have validation error for work Item {workItemId} originalId {originalId} - Field: {name} ErrorStatus {errorStatus} Value {value}", i, workItem.Id, itemToMigrate.OriginalId, error.Name, error.Status, error.Value);
                    }
                    return false;
                }
                workItem.Save();
            }

            return true;
        }

        private WorkItem GetWorkItem(String originalId)
        {
            var existingWorkItems = connection.WorkItemStore.Query($@"select * from  workitems where {fieldWithOriginalId} = '" + originalId + "'");
            foreach (WorkItem wi in existingWorkItems)
            {
                return wi;
            }

            return null;
        }

        private WorkItem CreateWorkItem(MigrationItem migrationItem)
        {

            var type = teamProject.WorkItemTypes[migrationItem.WorkItemDestinationType];
            if (type == null)
            {
                Log.Error("Unable to find work item type {WorkItemDestinationType}", migrationItem.WorkItemDestinationType);
                return null;
            }

            WorkItem workItem = new WorkItem(type);
            Log.Information("Created Work Item for type {workItemType} with {workItemId} related to original id {originalId}", workItem.Type.Name, workItem.Id, migrationItem.OriginalId);

            //now start creating basic value that we need, like the original id 
            workItem[fieldWithOriginalId] = migrationItem.OriginalId;
            return workItem;
        }
    }
}
