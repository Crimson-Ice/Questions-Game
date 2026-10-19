using System.Net;
using Quiz.Api.Tests.Infrastructure;

namespace Quiz.Api.Tests;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(QuizApiFactory factory)
{
    [Fact]
    public async Task GetHealth_ReturnsOkAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
