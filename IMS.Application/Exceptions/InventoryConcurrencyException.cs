using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Exceptions
{
    public class InventoryConcurrencyException : Exception
    {
        public InventoryConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
