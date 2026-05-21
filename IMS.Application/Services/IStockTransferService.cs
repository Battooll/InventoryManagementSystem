using IMS.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Services
{
    public interface IStockTransferService
    {
        Task<long> InitiateTransferAsync(InitiateTransferRequest request);
        Task CompleteTransferAsync(CompleteTransferRequest request);
    }
}
