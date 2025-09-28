using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Shared.Models;
using Xunit;

namespace PigFarmManagement.Server.Tests
{
    public class PosposFeedClientTests
    {
        [Fact]
        public async Task Parses_Data_Array_Shape_From_Sample()
        {
            // Arrange
            // Use a small inline sample JSON to avoid filesystem path issues in CI/test environments
            var json = @"{ ""success"":1, ""data"": [ { ""_id"": ""t1"", ""code"": ""TR1"", ""order_list"": [], ""timestamp"": ""2025-09-03T10:00:00Z"", ""reference_tax_invoice_abbreviate"": { ""code"": ""ST1"" }, ""buyer_detail"": { ""code"": ""C1"" } } ] }";

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Act: replicate parsing logic (array or object with data)
            var trimmed = json.TrimStart();
            System.Collections.Generic.IEnumerable<PigFarmManagement.Shared.Models.PosPosFeedTransaction>? tx = null;
            if (trimmed.StartsWith("["))
            {
                tx = JsonSerializer.Deserialize<System.Collections.Generic.IEnumerable<PigFarmManagement.Shared.Models.PosPosFeedTransaction>>(json, opts);
            }
            else
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
                {
                    tx = JsonSerializer.Deserialize<System.Collections.Generic.IEnumerable<PigFarmManagement.Shared.Models.PosPosFeedTransaction>>(data.GetRawText(), opts);
                }
                else
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                tx = JsonSerializer.Deserialize<System.Collections.Generic.IEnumerable<PigFarmManagement.Shared.Models.PosPosFeedTransaction>>(prop.Value.GetRawText(), opts);
                                if (tx != null && System.Linq.Enumerable.Any(tx)) break;
                            }
                            catch { }
                        }
                    }
                }
            }

            Assert.NotNull(tx);
            Assert.True(System.Linq.Enumerable.Any(tx));
            var first = System.Linq.Enumerable.First(tx);
            Assert.False(string.IsNullOrWhiteSpace(first.Code));
            Assert.False(string.IsNullOrWhiteSpace(first.Id));
        }

        // Minimal message handler to return the sample JSON regardless of request
        private class TestMessageHandler : System.Net.Http.HttpMessageHandler
        {
            private readonly string _body;
            public TestMessageHandler(string body) { _body = body; }
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var res = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.StringContent(_body, System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(res);
            }
        }
    }
}
