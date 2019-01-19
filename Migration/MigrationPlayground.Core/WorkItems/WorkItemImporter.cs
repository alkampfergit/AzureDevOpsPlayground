using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Serilog;
using System;
using System.Linq;
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
                    workItem.Fields["System.CreatedDate"].Value = version.VersionTimestamp;
                }
                workItem.Title = version.Title;
                workItem.Description = version.Description;
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
                if (i == 0)
                {
                    Log.Information("Saved for the first time Work Item for type {workItemType} with id {workItemId} related to original id {originalId}", workItem.Type.Name, workItem.Id, itemToMigrate.OriginalId);
                }
                else
                {
                    Log.Debug("Saved iteration {i} for original id {originalId}", i, itemToMigrate.OriginalId);
                }
            }

            return true;
        }

        private WorkItem GetWorkItem(String originalId)
        {
            var existingWorkItems = connection
                .WorkItemStore
                .Query($@"select * from  workitems where {fieldWithOriginalId} = '" + originalId + "'");
            return existingWorkItems.OfType<WorkItem>().FirstOrDefault();
        }

        private WorkItem CreateWorkItem(MigrationItem migrationItem)
        {
            WorkItemType type = null;
            try
            {
                type = teamProject.WorkItemTypes[migrationItem.WorkItemDestinationType];
            }
            catch (WorkItemTypeDeniedOrNotExistException) { }//ignore the exception will be logged  

            if (type == null)
            {
                Log.Error("Unable to find work item type {WorkItemDestinationType}", migrationItem.WorkItemDestinationType);
                return null;
            }

            WorkItem workItem = new WorkItem(type);
            Log.Information("Created Work Item for type {workItemType} related to original id {originalId}", workItem.Type.Name, migrationItem.OriginalId);

            //now start creating basic value that we need, like the original id 
            workItem[fieldWithOriginalId] = migrationItem.OriginalId;
            return workItem;
        }
    }
}
