using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Server.Models;

namespace PigFarmManagement.Server.Services
{
    public interface IPosposClient
    {
        Task<IEnumerable<PosposMember>> GetMembersAsync();
        Task<IEnumerable<PosposMember>> GetMembersByIdsAsync(IEnumerable<string> ids);
    }
}
