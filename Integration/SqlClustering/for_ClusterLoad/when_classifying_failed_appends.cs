// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_classifying_failed_appends : Specification
{
    const string CommandFailed = "Cratis.Chronicle.Contracts.Commands.CommandFailed";
    const string SiloUnavailableAppend = "Command failed: The target silo became unavailable for message: Request [source]->[target] Cratis.Chronicle.EventSequences.IEventSequence.Append(arguments)";

    [Fact]
    public void should_treat_an_append_to_an_unavailable_silo_as_uncertain() =>
        ClusterLoad.IsUnavailableSiloAppend(CommandFailed, SiloUnavailableAppend).ShouldBeTrue();

    [Theory]
    [InlineData("Command failed: An unrelated command failure")]
    [InlineData("Command failed: The target silo became unavailable")]
    [InlineData("Command failed: The target silo became unavailable for message: Request [source]->[target] OtherOperation(arguments)")]
    public void should_not_treat_other_command_failures_as_uncertain(string message) =>
        ClusterLoad.IsUnavailableSiloAppend(CommandFailed, message).ShouldBeFalse();

    [Fact]
    public void should_not_treat_other_exception_types_as_silo_unavailability() =>
        ClusterLoad.IsUncertainAppend(new InvalidOperationException(SiloUnavailableAppend)).ShouldBeFalse();

    [Fact]
    public void should_keep_transport_failure_uncertain() =>
        ClusterLoad.IsUncertainAppend(new RpcException(new(StatusCode.Unavailable, "Connection lost"))).ShouldBeTrue();

    [Fact]
    public void should_keep_timeout_uncertain() =>
        ClusterLoad.IsUncertainAppend(new TimeoutException()).ShouldBeTrue();
}
