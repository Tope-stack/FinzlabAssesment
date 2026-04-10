using FinzlabAssesment.Persistence;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinzlabAssesmentTests
{
    public class UnitTest1
    {

        private readonly HttpClient _client;

        public UnitTest1(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.Single(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(opt =>
                        opt.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
                });
            }).CreateClient();
        }

        private static object ValidPayload(string externalId = "ext-001") => new
        {
            externalId,
            amount = 500.00,
            currency = "EUR",
            status = "COMPLETED",
            occurredAt = "2024-06-01T10:00:00Z"
        };

        [Fact]
        public async Task Test1_NewTransaction_Returns201WithDerivedFields()
        {
            var response = await _client.PostAsJsonAsync("/webhooks/transactions", ValidPayload());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            Assert.NotNull(body);
            Assert.True(body.ContainsKey("amountUsd"));
            Assert.True(body.ContainsKey("riskLevel"));
        }

        [Fact]
        public async Task Test2_DuplicateTransaction_Returns200()
        {
            var payload = ValidPayload("ext-idempotency-test");

            var first = await _client.PostAsJsonAsync("/webhooks/transactions", payload);
            var second = await _client.PostAsJsonAsync("/webhooks/transactions", payload);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }
    }
}
