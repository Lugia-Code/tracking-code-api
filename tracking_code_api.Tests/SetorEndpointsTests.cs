using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using tracking_code_api.Dtos.SetorDtos;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace tracking_code_api.tracking_code_api.Tests;

public class SetorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SetorEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var rateLimiterDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(RateLimiterOptions));
                if (rateLimiterDescriptor != null)
                    services.Remove(rateLimiterDescriptor);
            });
        });
        
        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task GetSetores_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/setores");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSetorById_WhenNotExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/setores/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMotosBySetor_ReturnsOkOrNotFound()
    {
        var response = await _client.GetAsync("/api/v1/motos/setor/2?page=1&pageSize=10");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}