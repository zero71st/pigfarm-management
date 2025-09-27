using System.Collections.Generic;

namespace PigFarmManagement.Server.Services
{
    public interface IMappingStore
    {
        IDictionary<string, string> Load();
        void Save(IDictionary<string, string> map);
    }
}
