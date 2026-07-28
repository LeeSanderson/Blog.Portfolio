using Blog.Portfolio.Apps.Example.Backend.Ping;
using FluentAssertions;

namespace Blog.Portfolio.Apps.Example.Backend.Tests.Ping;

public class PingFunctionTests
{
    [Fact]
    public async Task HandleAsync_ReturnsPongMessage()
    {
        var function = new PingFunction();

        var response = await function.HandleAsync(new PingRequest(), CancellationToken.None);

        response.Message.Should().Be("pong");
    }
}
