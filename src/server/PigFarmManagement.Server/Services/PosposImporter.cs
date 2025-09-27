using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PigFarmManagement.Server.Models;
using PigFarmManagement.Shared.Models;
using Microsoft.Extensions.Logging;

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
        private readonly Infrastructure.Data.Repositories.ICustomerRepository _customerRepository;
        private readonly ILogger<PosposImporter> _logger;

        // in-memory transient store removed; persistence is handled via repository
        private IDictionary<string, string> _mapping;
        public PosposImportSummary? LastSummary { get; private set; }

        public PosposImporter(IPosposClient client, IMappingStore mappingStore, Infrastructure.Data.Repositories.ICustomerRepository customerRepository, ILogger<PosposImporter> logger)
        {
            _client = client;
            _mappingStore = mappingStore;
            _customerRepository = customerRepository;
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

                        // Map Pospos member to internal Customer model
                        PigFarmManagement.Shared.Models.Customer mapped = MapPosposToCustomer(m);

                        if (_mapping.TryGetValue(m.id, out var internalId))
                        {
                            // existing mapping -> update via repository
                            try
                            {
                                var withId = mapped with { Id = Guid.Parse(internalId) };
                                await _customerRepository.UpdateAsync(withId);
                                summary.Updated++;
                            }
                            catch (Exception ex)
                            {
                                // If update fails because not found, create instead
                                _logger.LogWarning(ex, "Update failed for mapped customer id {Id}, will try create", internalId);
                                var created = await _customerRepository.CreateAsync(mapped);
                                _mapping[m.id] = created.Id.ToString();
                                summary.Created++;
                            }
                        }
                        else
                        {
                            // create new via repository
                            var created = await _customerRepository.CreateAsync(mapped);
                            _mapping[m.id] = created.Id.ToString();
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var memberDisplay = GetMemberDisplay(m);
                        var err = $"Member id={m?.id ?? "(null)"} name={memberDisplay}: {ex.Message}";
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

                        PigFarmManagement.Shared.Models.Customer mapped = MapPosposToCustomer(m);

                        if (_mapping.TryGetValue(m.id, out var internalId))
                        {
                            try
                            {
                                var withId = mapped with { Id = Guid.Parse(internalId) };
                                await _customerRepository.UpdateAsync(withId);
                                summary.Updated++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Update failed for mapped customer id {Id}, will try create", internalId);
                                var created = await _customerRepository.CreateAsync(mapped);
                                _mapping[m.id] = created.Id.ToString();
                                summary.Created++;
                            }
                        }
                        else
                        {
                            var created = await _customerRepository.CreateAsync(mapped);
                            _mapping[m.id] = created.Id.ToString();
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var memberDisplay = GetMemberDisplay(m);
                        var err = $"Member id={m?.id ?? "(null)"} name={memberDisplay}: {ex.Message}";
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

        // Helper: Map PosposMember (external shape) to internal Shared.Customer model
        private PigFarmManagement.Shared.Models.Customer MapPosposToCustomer(PosposMember m)
        {
            // Prefer explicit FirstName/LastName fields
            string? first = string.IsNullOrWhiteSpace(m.FirstName) ? null : m.FirstName.Trim();
            string? last = string.IsNullOrWhiteSpace(m.LastName) ? null : m.LastName.Trim();

            // Use the external id as code if no other code is provided
            var code = !string.IsNullOrWhiteSpace(m.id) ? m.id : string.Empty;

            var cust = new PigFarmManagement.Shared.Models.Customer(Guid.NewGuid(), code, PigFarmManagement.Shared.Models.CustomerStatus.Active)
            {
                FirstName = first,
                LastName = last,
                ExternalId = m.id,
                Phone = string.IsNullOrWhiteSpace(m.phone) ? null : m.phone,
                Email = string.IsNullOrWhiteSpace(m.email) ? null : m.email,
                Address = string.IsNullOrWhiteSpace(m.address) ? null : m.address,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(m.createdAt).UtcDateTime,
                UpdatedAt = DateTime.UtcNow
            };

            return cust;
        }

        // Helper: produce a readable display name for logging from a PosposMember
        private string GetMemberDisplay(PosposMember? m)
        {
            if (m == null) return "(null)";
            var first = string.IsNullOrWhiteSpace(m.FirstName) ? string.Empty : m.FirstName.Trim();
            var last = string.IsNullOrWhiteSpace(m.LastName) ? string.Empty : m.LastName.Trim();
            var combined = (first + " " + last).Trim();
            if (!string.IsNullOrEmpty(combined)) return combined;
            if (!string.IsNullOrWhiteSpace(m.id)) return m.id;
            return "(unknown)";
        }
    }
}
