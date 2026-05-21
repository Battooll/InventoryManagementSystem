using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Enum
{
    public enum TransferStatus
    {
        Draft = 1,
        InTransit = 2,
        Completed = 3,
        Cancelled = 4
    }
}
