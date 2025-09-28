using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Shared.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace PigFarmManagement.Server.Tests
{
    public class PosposImporterTests
    {
        private class FakePosposClient : IPosposClient
        {
            private readonly IEnumerable<PosposMember> _members;
            public FakePosposClient(IEnumerable<PosposMember> members) => _members = members;
            public Task<IEnumerable<PosposMember>> GetMembersAsync() => Task.FromResult(_members);
            public Task<IEnumerable<PosposMember>> GetMembersByIdsAsync(IEnumerable<string> ids) => Task.FromResult(_members);
        }

        private class InMemoryCustomerRepository : ICustomerRepository
        {
            private readonly List<Customer> _store = new();
            public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(_store);
            public Task<Customer?> GetByIdAsync(Guid id) => Task.FromResult(_store.Find(c => c.Id == id));
            public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(_store.Find(c => c.Code == code));
            public Task<Customer> CreateAsync(Customer customer)
            {
                var toCreate = customer with { Id = Guid.NewGuid() };
                _store.Add(toCreate);
                return Task.FromResult(toCreate);
            }
            public Task<Customer> UpdateAsync(Customer customer)
            {
                var idx = _store.FindIndex(c => c.Id == customer.Id);
                if (idx >= 0) _store[idx] = customer;
                return Task.FromResult(customer);
            }
            public Task DeleteAsync(Guid id)
            {
                _store.RemoveAll(c => c.Id == id);
                return Task.CompletedTask;
            }
        }

        private class FakeMappingStore : IMappingStore
        {
            private Dictionary<string,string> _map = new();
            public IDictionary<string, string> Load() => new Dictionary<string,string>(_map);
            public void Save(IDictionary<string, string> mapping) => _map = new Dictionary<string,string>(mapping);
        }

        [Fact]
        public async Task MapPosposMember_To_Customer_Populates_Fields()
        {
            // Arrange: create a pospos member with name split and created_at ISO string
            var pos = new PosposMember
            {
                Id = "ext-123",
                Code = "C123",
                FirstName = "John",
                LastName = "Doe",
                Phone = "0123456789",
                Email = "john@example.com",
                Address = "123 Main St",
                KeyCardId = "K123",
                Sex = "male",
                Zipcode = "99999",
                CreatedAt = "2023-09-01T12:00:00Z"
            };

            var fakeClient = new FakePosposClient(new[] { pos });
            var mappingStore = new FakeMappingStore();
            var repo = new InMemoryCustomerRepository();
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<PosposImporter>();

            var importer = new PosposImporter(fakeClient, mappingStore, repo, logger);

            // Act: run import selected (this will call the internal MapPosposToCustomer)
            var summary = await importer.RunImportSelectedAsync(new[] { pos.Id }, persistMapping: false);

            // Assert
            Assert.Equal(1, summary.Created);
            var all = await repo.GetAllAsync();
            Assert.Single(all);
            var created = System.Linq.Enumerable.First(all);
            Assert.Equal("John", created.FirstName);
            Assert.Equal("Doe", created.LastName);
            Assert.Equal("C123", created.Code);
            Assert.Equal("ext-123", created.ExternalId);
            Assert.True(created.CreatedAt <= DateTime.UtcNow && created.CreatedAt > DateTime.UtcNow.AddYears(-5));
        }
    }
}
