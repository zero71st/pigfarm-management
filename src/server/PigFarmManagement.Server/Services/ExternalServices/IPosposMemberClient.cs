using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Domain.External;

namespace PigFarmManagement.Server.Services.ExternalServices
{
    public interface IPosposMemberClient
    {
        Task<IEnumerable<PosposMember>> GetMembersAsync();
        Task<IEnumerable<PosposMember>> GetMembersByIdsAsync(IEnumerable<string> ids);
    }
}