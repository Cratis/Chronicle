// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

/// <summary>
/// The handlers on this lifecycle are whole-artifact registrations - the event store's <c>RegisterAll</c> is one of
/// them - so running one twice re-registers every event type, constraint and seeding on every reconnect.
/// </summary>
/// <remarks>
/// It was a plain multicast event, so a caller that could not be sure whether it had already subscribed had no way
/// to express "subscribe unless already subscribed" other than removing first and adding back. The integration
/// fixture does exactly that, with a comment explaining that without the removal the handler count grows by one per
/// test class. Subscribing is idempotent now, so being unsure is no longer a hazard.
/// </remarks>
public class when_the_same_handler_subscribes_twice : Specification
{
    ConnectionLifecycle _lifecycle;
    int _invocations;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(Substitute.For<ILogger<ConnectionLifecycle>>());
        _lifecycle.OnConnected += Handler;
        _lifecycle.OnConnected += Handler;
        _lifecycle.OnConnected += Handler;
    }

    async Task Because() => await _lifecycle.Connected();

    [Fact] void should_invoke_it_once() => _invocations.ShouldEqual(1);

    [Fact]
    async Task should_still_be_removable()
    {
        _lifecycle.OnConnected -= Handler;
        _invocations = 0;
        await _lifecycle.Connected();
        _invocations.ShouldEqual(0);
    }

    [Fact]
    async Task should_keep_a_different_handler()
    {
        var other = 0;
        _lifecycle.OnConnected += () =>
        {
            other++;
            return Task.CompletedTask;
        };
        _invocations = 0;
        await _lifecycle.Connected();
        _invocations.ShouldEqual(1);
        other.ShouldEqual(1);
    }

    Task Handler()
    {
        _invocations++;
        return Task.CompletedTask;
    }
}
