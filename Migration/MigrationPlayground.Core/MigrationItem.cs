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

        public String OriginalId { get; set; }

        private readonly List<MigrationItemVersion> _versions;

        public IEnumerable<MigrationItemVersion> Versions => _versions;

        public MigrationItem AddVersion(MigrationItemVersion version)
        {
            _versions.Add(version);
            return this;
        }
    }
}
