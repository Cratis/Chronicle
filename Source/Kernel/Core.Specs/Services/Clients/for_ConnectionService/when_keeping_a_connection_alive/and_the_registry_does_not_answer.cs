// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// Characterizes today's behavior: a keep-alive the client answers waits for the registry grain, so a saturated
/// registry holds the answer back while the kernel's own keep-alive stream carries on.
/// </summary>
public class and_the_registry_does_not_answer : given.a_connected_client
{
    Task _response;
    bool _responseCompleted;

    async Task Because()
    {
        await ConnectAndWaitForRegistration();
        _connectedClients.OnClientPing(new ConnectionId(TheConnection)).Returns(new TaskCompletionSource<bool>().Task);
        var sentBefore = KeepAlivesSent;
        _response = Respond();
        try
        {
            await _response.WaitAsync(TimeSpan.FromMilliseconds(500));
        }
        catch (TimeoutException)
        {
            // Expected: the registry never answers.
        }

        _responseCompleted = _response.IsCompleted;
        await WaitUntil(() => KeepAlivesSent > sentBefore);
    }

    [Fact] void should_wait_for_the_registry_before_answering() => _responseCompleted.ShouldBeFalse();
    [Fact] void should_keep_sending_keep_alives_on_the_stream() => KeepAlivesSent.ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_not_complete_the_stream() => Completed.ShouldBeFalse();
}
