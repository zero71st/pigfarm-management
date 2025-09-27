using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PigFarmManagement.Server.Models;

namespace PigFarmManagement.Server.Services
{
    public class PosposImportSummary
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface IPosposImporter
    {
        Task<PosposImportSummary> RunImportAsync(bool persistMapping = false);
        PosposImportSummary? LastSummary { get; }
    }

    public class PosposImporter : IPosposImporter
    {
        private readonly IPosposClient _client;
        private readonly IMappingStore _mappingStore;
        private readonly Dictionary<string, Customer> _customers = new Dictionary<string, Customer>();
        private IDictionary<string, string> _mapping;
        public PosposImportSummary? LastSummary { get; private set; }

        public PosposImporter(IPosposClient client, IMappingStore mappingStore)
        {
            _client = client;
            _mappingStore = mappingStore;
            _mapping = _mappingStore.Load();
        }

        public async Task<PosposImportSummary> RunImportAsync(bool persistMapping = false)
        {
            var summary = new PosposImportSummary();
            try
            {
                var members = await _client.GetMembersAsync();
                foreach (var m in members)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(m.id))
                        {
                            summary.Skipped++;
                            continue;
                        }

                        if (_mapping.TryGetValue(m.id, out var internalId))
                        {
                            // update existing
                            if (_customers.TryGetValue(internalId, out var existing))
                            {
                                existing.Name = m.name;
                                existing.Phone = m.phone;
                                existing.Email = m.email;
                                existing.Address = m.address;
                                summary.Updated++;
                            }
                            else
                            {
                                // mapping exists but customer missing -> create
                                var c = new Customer { Name = m.name, Phone = m.phone, Email = m.email, Address = m.address };
                                _customers[c.Id] = c;
                                _mapping[m.id] = c.Id;
                                summary.Created++;
                            }
                        }
                        else
                        {
                            // create new internal customer
                            var c = new Customer { Name = m.name, Phone = m.phone, Email = m.email, Address = m.address };
                            _customers[c.Id] = c;
                            _mapping[m.id] = c.Id;
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        summary.Errors.Add(ex.Message);
                    }
                }

                if (persistMapping)
                {
                    _mappingStore.Save(_mapping);
                }
            }
            catch (Exception ex)
            {
                summary.Errors.Add(ex.Message);
            }

            summary.Timestamp = DateTime.UtcNow;
            LastSummary = summary;
            return summary;
        }
    }
}
