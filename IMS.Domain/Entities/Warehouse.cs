using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class Warehouse : BaseEntity
    {        
        public string Name { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
