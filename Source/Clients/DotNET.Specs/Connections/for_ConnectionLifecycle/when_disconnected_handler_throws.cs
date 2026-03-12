// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_disconnected_handler_throws : Specification
{
    ConnectionLifecycle _lifecycle;
    bool _secondHandlerCalled;

    void Establish()
    {
        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);

        // First handler throws
        _lifecycle.OnDisconnected += () => throw new InvalidOperationException("handler failure");

        // Second handler should still be called
        _lifecycle.OnDisconnected += () =>
        {
            _secondHandlerCalled = true;
            return Task.CompletedTask;
        };

        _lifecycle.Connected().GetAwaiter().GetResult();
    }

    async Task Because() => await _lifecycle.Disconnected();

    [Fact] void should_still_call_remaining_handlers() => _secondHandlerCalled.ShouldBeTrue();
    [Fact] void should_mark_as_not_connected() => _lifecycle.IsConnected.ShouldBeFalse();
}
