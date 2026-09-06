// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

/// <summary>
/// A kernel that has not come up yet while its client starts is ordinary, and the client is expected to keep
/// trying rather than fail whoever asked first. The failure state #3948 introduces is for a connection that
/// worked and then stopped recovering, so it must not arm before the first successful connect - a host that
/// cannot boot through a slow-starting kernel is worse than one that keeps trying.
/// </summary>
public class and_the_kernel_is_not_up_yet_for_the_first_connect : given.a_connection_that_lost_a_working_kernel
{
    Exception _first;
    Exception _second;

    async Task Because()
    {
        _kernelIsReachable = false;
        _first = await Catch.Exception(_connection.Connect);
        _second = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_surface_the_failure_to_the_first_caller() => _first.ShouldBeOfExactType<UnableToResolveClientUri>();

    [Fact] void should_not_arm_the_back_off_before_a_connection_has_ever_been_made() =>
        _second.ShouldBeOfExactType<UnableToResolveClientUri>();

    [Fact] void should_keep_attempting_for_every_caller() => _resolveAttempts.ShouldEqual(2);
}
