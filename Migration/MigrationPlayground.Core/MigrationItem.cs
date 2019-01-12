using System;
using System.Collections.Generic;
using System.Text;

namespace MigrationPlayground.Core
{
    public class MigrationItem
    {
        public MigrationItem()
        {
            _versions = new List<MigrationItemVersion>();
        }

        /// <summary>
        /// This is the original Id of original system to keep track of what was already
        /// imported.
        /// </summary>
        public String OriginalId { get; set; }

        /// <summary>
        /// You need to specify type of workitem to be used during import.
        /// </summary>
        public String WorkItemDestinationType { get; set; }

        private readonly List<MigrationItemVersion> _versions;

        /// <summary>
        /// This is the list of all the version that this item had in the past, it is
        /// needed to allow importing the whole history of a work item.
        /// </summary>
        public IEnumerable<MigrationItemVersion> Versions => _versions;

        public MigrationItemVersion GetVersionAt(Int32 position)
        {
            return _versions[position];
        }

        public MigrationItem AddVersion(MigrationItemVersion version)
        {
            _versions.Add(version);
            return this;
        }
    }
}
