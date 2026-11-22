// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// A keep-alive the client answers is answered within the grace whatever the registry grain is doing; the registry's
/// bookkeeping carries on in the background. The kernel's own keep-alive stream carries on meanwhile too.
/// </summary>
public class and_the_registry_does_not_answer : given.a_connected_client
{
    readonly TaskCompletionSource<bool> _ping = new();
    Task _response;
    bool _responseCompleted;

    async Task Because()
    {
        await ConnectAndWaitForRegistration();
        _connectedClients.OnClientPing(new ConnectionId(TheConnection)).Returns(_ping.Task);
        var sentBefore = KeepAlivesSent;
        _response = Respond();
        try
        {
            await _response.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            // Reported by the assertion below.
        }

        _responseCompleted = _response.IsCompletedSuccessfully;
        await WaitUntil(() => KeepAlivesSent > sentBefore);
    }

    void Destroy() => _ping.TrySetResult(true);

    [Fact] void should_answer_without_waiting_for_the_registry() => _responseCompleted.ShouldBeTrue();
    [Fact] void should_keep_sending_keep_alives_on_the_stream() => KeepAlivesSent.ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_not_complete_the_stream() => Completed.ShouldBeFalse();
}
