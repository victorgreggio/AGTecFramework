using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AGTec.Ai;

/// <summary>
/// A deterministic, no-model <see cref="IChatClient"/> — the local default so agent/chat code runs
/// end to end with no model or cloud. It echoes the last user message with a <c>[stub]</c> marker.
/// An OpenAI/Azure client replaces it behind the same abstraction, selected by configuration.
/// </summary>
public sealed class StubChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Reply(messages))));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, Reply(messages));
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    private static string Reply(IEnumerable<ChatMessage> messages)
    {
        var lastUser = messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
        return string.IsNullOrEmpty(lastUser) ? "[stub] (no input)" : $"[stub] {lastUser}";
    }
}
