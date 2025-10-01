using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace tracking_code_api.tracking_code_api.Tests;

public class MotoEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MotoEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove rate limiting para testes
                var rateLimiterDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(RateLimiterOptions));
                if (rateLimiterDescriptor != null)
                    services.Remove(rateLimiterDescriptor);
            });
        });
        
        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task GetMotos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/motos?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTagsDisponiveis_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/motos/tags-disponiveis");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSetores_ReturnsOk()
    {
        var response = await _client.GetAsync("/setores");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTags_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/tags");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsuarios_ReturnsOk()
    {
        var response = await _client.GetAsync("/usuarios");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task GetMotoByInvalidChassi_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/motos/buscar/chassi/INVALID123");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}