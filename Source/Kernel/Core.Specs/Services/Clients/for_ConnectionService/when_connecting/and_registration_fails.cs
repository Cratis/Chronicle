// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_fails : given.a_gated_connection_service
{
    readonly InvalidOperationException _failure = new("registration failed");
    Exception _error;

    void Establish() => _clients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
        .Returns(Task.FromException(_failure));

    async Task Because()
    {
        var stream = Start(new CancellationTokenSource());
        _error = (await stream.Completed.WaitAsync(TimeSpan.FromSeconds(5)))!;
        using var gate = await _ownership.Enter("connection");
    }

    [Fact] void should_preserve_the_registration_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_release_the_failed_claim() => _ownership.GetClaim("connection").ShouldBeNull();
    [Fact] void should_not_leave_an_open_connection() => _openConnections.Responded("connection", false, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), out _).ShouldBeFalse();
}
