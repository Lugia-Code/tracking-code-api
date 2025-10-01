using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using tracking_code_api.Dtos.TagDtos;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace tracking_code_api.tracking_code_api.Tests;

public class TagEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TagEndpointsTests(WebApplicationFactory<Program> factory)
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
    public async Task GetTags_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/v1/tags");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTagByCodigo_WhenNotExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/tags/TAG_INEXISTENTE_999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTag_WhenNotExists_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/v1/tags/TAG_INEXISTENTE_DEL");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}