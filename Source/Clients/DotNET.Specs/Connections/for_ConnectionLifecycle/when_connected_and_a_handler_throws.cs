// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle;

public class when_connected_and_a_handler_throws : Specification
{
    ConnectionLifecycle _lifecycle;
    bool _subsequentHandlerCalled;
    Exception _thrownException;
    Exception _error;

    void Establish()
    {
        _thrownException = new InvalidOperationException("Registration failed");
        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);

        _lifecycle.OnConnected += () => throw _thrownException;
        _lifecycle.OnConnected += () =>
        {
            _subsequentHandlerCalled = true;
            return Task.CompletedTask;
        };
    }

    async Task Because() => _error = await Catch.Exception(async () => await _lifecycle.Connected());

    [Fact] void should_not_mark_as_connected() => _lifecycle.IsConnected.ShouldBeFalse();
    [Fact] void should_still_invoke_subsequent_handlers() => _subsequentHandlerCalled.ShouldBeTrue();
    [Fact] void should_throw() => _error.ShouldNotBeNull();
    [Fact] void should_throw_aggregate_exception() => _error.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_include_the_handler_exception() => ((AggregateException)_error).InnerExceptions.ShouldContain(_thrownException);
}
