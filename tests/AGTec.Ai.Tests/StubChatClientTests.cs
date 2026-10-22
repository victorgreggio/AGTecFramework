using AwesomeAssertions;
using Microsoft.Extensions.AI;
using AGTec.Ai;

namespace AGTec.Ai.Tests;

public class StubChatClientTests
{
    [Fact]
    public async Task Echoes_the_last_user_message()
    {
        IChatClient client = new StubChatClient();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello world")]);

        response.Text.Should().StartWith("[stub]").And.Contain("hello world");
    }

    [Fact]
    public async Task Streams_the_reply()
    {
        IChatClient client = new StubChatClient();

        var text = string.Empty;
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "ping")]))
        {
            text += update.Text;
        }

        text.Should().Contain("ping");
    }
}
