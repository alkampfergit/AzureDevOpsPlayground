using System;
using System.Collections.Generic;
using System.Text;

namespace MigrationPlayground.Core
{
    /// <summary>
    /// This represents a single version of the item, a <see cref="MigrationItem"/>
    /// is composed by more than one version, each one represents a change made by a user.
    /// </summary>
    public class MigrationItemVersion
    {
        public String Title { get; set; }

        public String Description { get; set; }

        public String AuthorEmail { get; set; }

        public DateTime? VersionTimestamp { get; set; }
    }
}
