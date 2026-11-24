// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_compatibility_times_out_after_a_timed_out_handshake : given.a_connection_with_compatibility_timeouts
{
    IServices _initialServices;
    Exception _error;

    async Task Because()
    {
        _compatibility.Clear();
        var accessing = _connection.GetServices(CancellationToken.None).AsTask();
        await CompleteTimedOutAttempt(accessing);
        _initialServices = await accessing;

        StallCompatibilityChecks();
        _error = await Catch.Exception(() => CompleteTimedOutAttempt(_connection.GetServices(CancellationToken.None).AsTask()));
    }

    [Fact] void should_allow_services_from_the_current_channel_after_handshake_timeout() => _initialServices.ShouldNotBeNull();
    [Fact] void should_not_return_services_from_the_abandoned_channel() => _error.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
}
