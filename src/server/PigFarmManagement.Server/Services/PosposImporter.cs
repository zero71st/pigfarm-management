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
        Task<PosposImportSummary> RunImportSelectedAsync(IEnumerable<string> posposIds, bool persistMapping = false);
        PosposImportSummary? LastSummary { get; }
    }

    public class PosposImporter : IPosposImporter
    {
        private readonly IPosposClient _client;
        private readonly IMappingStore _mappingStore;
        private readonly Microsoft.Extensions.Logging.ILogger<PosposImporter> _logger;
        private readonly Dictionary<string, Customer> _customers = new Dictionary<string, Customer>();
        private IDictionary<string, string> _mapping;
        public PosposImportSummary? LastSummary { get; private set; }

        public PosposImporter(IPosposClient client, IMappingStore mappingStore, Microsoft.Extensions.Logging.ILogger<PosposImporter> logger)
        {
            _client = client;
            _mappingStore = mappingStore;
            _logger = logger;
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
                        var err = $"Member id={m?.id ?? "(null)"} name={m?.name ?? "(null)"}: {ex.Message}";
                        summary.Errors.Add(err);
                        _logger.LogError(ex, "Error importing member {MemberId}", m?.id);
                    }
                }

                if (persistMapping)
                {
                    try
                    {
                        _mappingStore.Save(_mapping);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist mapping");
                        summary.Errors.Add($"Failed to persist mapping: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunImportAsync failed");
                summary.Errors.Add($"Import failed: {ex.Message}");
            }

            summary.Timestamp = DateTime.UtcNow;
            LastSummary = summary;
            _logger.LogInformation("Import completed. created={Created} updated={Updated} skipped={Skipped} errors={ErrorsCount}", summary.Created, summary.Updated, summary.Skipped, summary.Errors.Count);
            return summary;
        }

        public async Task<PosposImportSummary> RunImportSelectedAsync(IEnumerable<string> posposIds, bool persistMapping = false)
        {
            var summary = new PosposImportSummary();
            try
            {
                var members = await _client.GetMembersByIdsAsync(posposIds);
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
                                var c = new Customer { Name = m.name, Phone = m.phone, Email = m.email, Address = m.address };
                                _customers[c.Id] = c;
                                _mapping[m.id] = c.Id;
                                summary.Created++;
                            }
                        }
                        else
                        {
                            var c = new Customer { Name = m.name, Phone = m.phone, Email = m.email, Address = m.address };
                            _customers[c.Id] = c;
                            _mapping[m.id] = c.Id;
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var err = $"Member id={m?.id ?? "(null)"} name={m?.name ?? "(null)"}: {ex.Message}";
                        summary.Errors.Add(err);
                        _logger.LogError(ex, "Error importing member {MemberId}", m?.id);
                    }
                }

                if (persistMapping)
                {
                    try
                    {
                        _mappingStore.Save(_mapping);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist mapping");
                        summary.Errors.Add($"Failed to persist mapping: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunImportSelectedAsync failed");
                summary.Errors.Add($"Import failed: {ex.Message}");
            }

            summary.Timestamp = DateTime.UtcNow;
            LastSummary = summary;
            _logger.LogInformation("Selected import completed. created={Created} updated={Updated} skipped={Skipped} errors={ErrorsCount}", summary.Created, summary.Updated, summary.Skipped, summary.Errors.Count);
            return summary;
        }
    }
}
