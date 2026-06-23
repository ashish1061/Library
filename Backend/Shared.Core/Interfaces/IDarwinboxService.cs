using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Core.Domain;

namespace Shared.Core.Interfaces
{
    public interface IDarwinboxService
    {
        Task<List<Employee>> GetEmployeesAsync();
        Task<string> GetProfilePicAsync(string employeeNo);
    }
}
