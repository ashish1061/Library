using System;
using System.Threading.Tasks;
using Shared.Core.Domain;

namespace Shared.Core.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<int> LogActionAsync(string action, string empId, string entity, string entityId, string details);
    }
}
