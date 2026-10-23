// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_classifying_failed_appends : Specification
{
    const string CommandFailed = "Cratis.Chronicle.Contracts.Commands.CommandFailed";
    const string RefusedByDepartedSilo = "Command failed: Exception while sending message: Orleans.Runtime.Messaging.ConnectionFailedException: Unable to connect to endpoint S172.20.0.5:11111:151569171. See InnerException\n ---> Orleans.Networking.Shared.SocketConnectionException: Unable to connect to 172.20.0.5:11111. Error: ConnectionRefused";
    const string BackingOffFromDepartedSilo = "Command failed: Exception while sending message: Orleans.Runtime.Messaging.ConnectionFailedException: Unable to connect to S172.20.0.5:11111:151569171, will retry after 903.4473ms";
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

    [Fact]
    public void should_treat_a_refused_forward_to_a_silo_the_fixture_took_down_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, RefusedByDepartedSilo, [("172.20.0.5", 151569171)]).ShouldBeTrue();

    [Fact]
    public void should_not_treat_a_refused_forward_to_another_silo_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, RefusedByDepartedSilo, [("172.20.0.4", 151569171)]).ShouldBeFalse();

    [Fact]
    public void should_not_treat_a_refused_forward_to_a_later_incarnation_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, RefusedByDepartedSilo, [("172.20.0.5", 151569200)]).ShouldBeFalse();

    [Fact]
    public void should_not_treat_another_connection_failure_to_a_departed_silo_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, RefusedByDepartedSilo.Replace("ConnectionRefused", "HostUnreachable"), [("172.20.0.5", 151569171)]).ShouldBeFalse();

    [Fact]
    public void should_treat_a_forward_to_a_departed_silo_while_orleans_backs_off_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, BackingOffFromDepartedSilo, [("172.20.0.5", 151569171)]).ShouldBeTrue();

    [Fact]
    public void should_not_treat_backing_off_from_another_silo_as_uncertain() =>
        ClusterLoad.IsAppendToDepartedSilo(CommandFailed, BackingOffFromDepartedSilo, [("172.20.0.4", 151569171)]).ShouldBeFalse();
}
