using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string SKU { get; set; } = string.Empty; // Stock Keeping Unit (Will be indexed/unique)
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
