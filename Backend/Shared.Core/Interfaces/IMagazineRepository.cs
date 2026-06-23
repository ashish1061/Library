using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Core.Domain;

namespace Shared.Core.Interfaces
{
    public interface IMagazineRepository
    {
        Task<IEnumerable<Magazine>> GetAllMagazinesAsync();
        Task<Magazine?> GetMagazineByIdAsync(long magazineId);
        Task<int> AddMagazineAsync(Magazine magazine);
        Task<int> UpdateMagazineAsync(Magazine magazine);
        Task<int> DeleteMagazineAsync(long magazineId);
        Task<IEnumerable<Magazine>> SearchMagazinesAsync(string category, string keyword);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}
