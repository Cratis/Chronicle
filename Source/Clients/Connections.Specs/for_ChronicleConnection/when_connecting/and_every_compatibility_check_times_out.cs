// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_every_compatibility_check_times_out : given.a_connection_with_compatibility_timeouts
{
    Exception _connectError;
    Exception _asynchronousAccessError;
    Exception _synchronousAccessError;

    async Task Because()
    {
        _connectError = await Catch.Exception(() => CompleteTimedOutAttempt(StartConnecting()));
        _asynchronousAccessError = await Catch.Exception(() => CompleteTimedOutAttempt(_connection.GetServices(CancellationToken.None).AsTask()));
        var accessing = Task.Run(() => ((IChronicleServicesAccessor)_connection).Services);
        _synchronousAccessError = await Catch.Exception(() => CompleteTimedOutAttempt(accessing));
    }

    [Fact] void should_keep_initial_connect_permissive() => _connectError.ShouldBeNull();
    [Fact] void should_fail_asynchronous_service_access_with_a_connection_timeout() => _asynchronousAccessError.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_fail_synchronous_service_access_with_a_connection_timeout() => _synchronousAccessError.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
    [Fact] void should_not_create_a_keep_alive_stream() => _servedBy.ShouldBeEmpty();
    [Fact] void should_start_one_monitor() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_arm_a_backoff_for_initial_failure() => _resolver.Received(3).Resolve(Arg.Any<ChronicleConnectionString>());
}
