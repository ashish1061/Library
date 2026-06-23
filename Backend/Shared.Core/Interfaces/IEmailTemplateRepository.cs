using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Core.Domain;

namespace Shared.Core.Interfaces
{
    public interface IEmailTemplateRepository
    {
        Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync();
        Task<EmailTemplate?> GetTemplateByPurposeAsync(string purpose);
        Task<EmailTemplate?> GetTemplateByIdAsync(int id);
        Task<int> UpsertTemplateAsync(EmailTemplate template);
        Task<int> DeleteTemplateAsync(int id);
    }
}
