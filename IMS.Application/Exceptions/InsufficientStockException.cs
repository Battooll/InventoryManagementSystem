using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string productName, int requested, int available)
            : base($"Insufficient stock for product '{productName}'. Requested: {requested}, Available: {available}.")
        {
        }
    }
}
