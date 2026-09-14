// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

/// <summary>
/// The back-off keeps callers off an unreachable kernel, but the watchdog is what brings the connection back and
/// paces itself with its own retry schedule. Holding it to the caller-facing back-off refused it before it dialed
/// anything, so it burned its attempts and the connection could never recover on its own.
/// </summary>
public class and_the_watchdog_reconnects_within_the_back_off : given.a_connection_that_lost_a_working_kernel
{
    Exception _callerResult;

    async Task Because()
    {
        await ConnectThenLoseTheKernel();

        // A caller fails and starts the back-off.
        await Catch.Exception(_connection.Connect);

        // The watchdog reconnects immediately afterwards - well inside the window a caller is refused in.
        await Catch.Exception(_connection.Reconnect);

        _callerResult = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_let_the_watchdog_attempt() => _resolveAttempts.ShouldEqual(2);
    [Fact] void should_still_refuse_a_caller_within_the_back_off() => _callerResult.ShouldBeOfExactType<ConnectionUnavailable>();
}
