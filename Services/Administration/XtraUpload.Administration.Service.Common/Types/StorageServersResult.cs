﻿using System.Collections.Generic;
using XtraUpload.Domain;

namespace XtraUpload.Administration.Service.Common
{
    public class StorageServersResult : OperationResult
    {
        public IEnumerable<StorageServer> Servers { get; set; }
    }
}
