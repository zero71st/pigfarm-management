using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Domain.External;
using PigFarmManagement.Shared.Models;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Features.Customers
{
    public class CustomerImportSummary
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface ICustomerImportService
    {
        Task<CustomerImportSummary> ImportAllCustomersAsync();
        Task<CustomerImportSummary> ImportSelectedCustomersAsync(IEnumerable<string> posposIds);
        CustomerImportSummary? LastImportSummary { get; }
    }

    public class CustomerImportService : ICustomerImportService
    {
        private readonly IPosposMemberClient _client;
        private readonly Infrastructure.Data.Repositories.ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerImportService> _logger;

        public CustomerImportSummary? LastImportSummary { get; private set; }

        public CustomerImportService(IPosposMemberClient client, Infrastructure.Data.Repositories.ICustomerRepository customerRepository, ILogger<CustomerImportService> logger)
        {
            _client = client;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<CustomerImportSummary> ImportAllCustomersAsync()
        {
            var summary = new CustomerImportSummary();
            try
            {
                var members = await _client.GetMembersAsync();
                var memberIds = members.Where(m => !string.IsNullOrEmpty(m.Id)).Select(m => m.Id!).ToList();
                
                // Batch fetch existing customers by external id
                var existingCustomers = (await _customerRepository.GetByExternalIdsAsync(memberIds)).ToList();
                var existingMap = existingCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.ExternalId))
                    .ToDictionary(c => c.ExternalId!, c => c.Id);

                foreach (var m in members)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(m.Id))
                        {
                            summary.Skipped++;
                            continue;
                        }

                        // Map Pospos member to internal Customer model
                        PigFarmManagement.Shared.Models.Customer mapped = MapPosposToCustomer(m);

                        if (existingMap.TryGetValue(m.Id, out var internalId))
                        {
                            // existing customer -> update via repository, preserving location data
                            try
                            {
                                var withId = mapped with { Id = internalId };
                                await UpdateCustomerPreservingLocationAsync(withId);
                                summary.Updated++;
                            }
                            catch (Exception ex)
                            {
                                // If update fails because not found, create instead
                                _logger.LogWarning(ex, "Update failed for mapped customer id {Id}, will try create", internalId);
                                var created = await _customerRepository.CreateAsync(mapped.ToCreateDto());
                                existingMap[m.Id] = created.Id; // keep in-memory map for this run
                                summary.Created++;
                            }
                        }
                        else
                        {
                            // create new via repository
                            var created = await _customerRepository.CreateAsync(mapped.ToCreateDto());
                            existingMap[m.Id] = created.Id; // keep in-memory map for this run
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var memberDisplay = GetMemberDisplay(m);
                        var err = $"Member id={m?.Id ?? "(null)"} name={memberDisplay}: {ex.Message}";
                        summary.Errors.Add(err);
                        _logger.LogError(ex, "Error importing member {MemberId}", m?.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunImportAsync failed");
                summary.Errors.Add($"Import failed: {ex.Message}");
            }

            summary.Timestamp = DateTime.UtcNow;
            LastImportSummary = summary;
            _logger.LogInformation("Import completed. created={Created} updated={Updated} skipped={Skipped} errors={ErrorsCount}", summary.Created, summary.Updated, summary.Skipped, summary.Errors.Count);
            return summary;
        }

        public async Task<CustomerImportSummary> ImportSelectedCustomersAsync(IEnumerable<string> posposIds)
        {
            var summary = new CustomerImportSummary();
            try
            {
                var ids = posposIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? new List<string>();
                if (!ids.Any())
                {
                    _logger.LogWarning("No valid POSPOS IDs provided for import");
                    return summary;
                }

                // Batch fetch existing customers by external id
                var existingCustomers = (await _customerRepository.GetByExternalIdsAsync(ids)).ToList();
                var existingMap = existingCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.ExternalId))
                    .ToDictionary(c => c.ExternalId!, c => c.Id);

                var members = await _client.GetMembersByIdsAsync(ids);
                foreach (var m in members)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(m.Id))
                        {
                            summary.Skipped++;
                            continue;
                        }

                        PigFarmManagement.Shared.Models.Customer mapped = MapPosposToCustomer(m);

                        if (existingMap.TryGetValue(m.Id, out var internalId))
                        {
                            try
                            {
                                var withId = mapped with { Id = internalId };
                                await UpdateCustomerPreservingLocationAsync(withId);
                                summary.Updated++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Update failed for mapped customer id {Id}, will try create", internalId);
                                var created = await _customerRepository.CreateAsync(mapped.ToCreateDto());
                                existingMap[m.Id] = created.Id; // keep in-memory map for this run
                                summary.Created++;
                            }
                        }
                        else
                        {
                            var created = await _customerRepository.CreateAsync(mapped.ToCreateDto());
                            existingMap[m.Id] = created.Id; // keep in-memory map for this run
                            summary.Created++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var memberDisplay = GetMemberDisplay(m);
                        var err = $"Member id={m?.Id ?? "(null)"} name={memberDisplay}: {ex.Message}";
                        summary.Errors.Add(err);
                        _logger.LogError(ex, "Error importing member {MemberId}", m?.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunImportSelectedAsync failed");
                summary.Errors.Add($"Import failed: {ex.Message}");
            }

            summary.Timestamp = DateTime.UtcNow;
            LastImportSummary = summary;
            _logger.LogInformation("Selected import completed. created={Created} updated={Updated} skipped={Skipped} errors={ErrorsCount}", summary.Created, summary.Updated, summary.Skipped, summary.Errors.Count);
            return summary;
        }

        // Helper: Map PosposMember (external shape) to internal Shared.Customer model
        private PigFarmManagement.Shared.Models.Customer MapPosposToCustomer(PosposMember m)
        {
            // Prefer explicit FirstName/LastName fields
            string? first = string.IsNullOrWhiteSpace(m.FirstName) ? null : m.FirstName.Trim();
            string? last = string.IsNullOrWhiteSpace(m.LastName) ? null : m.LastName.Trim();

            // Prefer the POSPOS 'Code' (e.g. M0000X). Fall back to the external id when Code is not present.
            var code = !string.IsNullOrWhiteSpace(m.Code) ? m.Code : (m.Id ?? string.Empty);

            var cust = new PigFarmManagement.Shared.Models.Customer(Guid.NewGuid(), code, PigFarmManagement.Shared.Models.CustomerStatus.Active)
            {
                FirstName = first,
                LastName = last,
                ExternalId = m.Id,
                KeyCardId = string.IsNullOrWhiteSpace(m.KeyCardId) ? null : m.KeyCardId,
                Phone = string.IsNullOrWhiteSpace(m.Phone) ? (string.IsNullOrWhiteSpace(m.PhoneNumber) ? null : m.PhoneNumber) : m.Phone,
                Email = string.IsNullOrWhiteSpace(m.Email) ? null : m.Email,
                Address = string.IsNullOrWhiteSpace(m.Address) ? null : m.Address,
                Sex = string.IsNullOrWhiteSpace(m.Sex) ? null : m.Sex,
                Zipcode = string.IsNullOrWhiteSpace(m.Zipcode) ? null : m.Zipcode,
                // CreatedAt from POSPOS can be numeric (ms since epoch) or ISO string; try both
                CreatedAt = ParseCreatedAt(m.CreatedAt),
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
            if (!string.IsNullOrWhiteSpace(m.Id)) return m.Id;
            if (!string.IsNullOrWhiteSpace(m.Code)) return m.Code;
            return "(unknown)";
        }

        private static DateTime ParseCreatedAt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DateTime.UtcNow;
            // Try ISO parse
            if (DateTimeOffset.TryParse(value, out var dto)) return dto.UtcDateTime;

            // Try numeric milliseconds
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (long.TryParse(digits, out var ms))
            {
                try { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; }
                catch { }
            }

            return DateTime.UtcNow;
        }

        /// <summary>
        /// Updates customer from POS data while preserving location fields (Latitude, Longitude)
        /// </summary>
        private async Task UpdateCustomerPreservingLocationAsync(PigFarmManagement.Shared.Models.Customer updatedCustomer)
        {
            // Get existing customer to preserve location data
            var existingCustomer = await _customerRepository.GetByIdAsync(updatedCustomer.Id);
            if (existingCustomer == null)
            {
                throw new ArgumentException($"Customer with ID {updatedCustomer.Id} not found");
            }

            // Create updated customer preserving location fields
            var customerWithPreservedLocation = updatedCustomer with 
            { 
                Latitude = existingCustomer.Latitude,
                Longitude = existingCustomer.Longitude
            };

            await _customerRepository.UpdateAsync(customerWithPreservedLocation.Id, customerWithPreservedLocation.ToUpdateDto());
        }
    }
}
