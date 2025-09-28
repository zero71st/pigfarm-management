using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PigFarmManagement.Server.Services
{
    public class FileMappingStore : IMappingStore
    {
        private readonly string _path;

        public FileMappingStore()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "customer_id_mapping.json");
        }

        public IDictionary<string, string> Load()
        {
            if (!File.Exists(_path)) return new Dictionary<string, string>();
            var json = File.ReadAllText(_path);
            try
            {
                var d = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return d ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public void Save(IDictionary<string, string> map)
        {
            var tmp = _path + ".tmp";
            var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, json);
            if (File.Exists(_path))
            {
                var bak = _path + ".bak";
                File.Copy(_path, bak, true);
            }
            File.Move(tmp, _path, true);
        }
    }
}
