// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_denied_system_operations_are_enforced : given.an_operation_authorization_policy
{
    readonly List<Exception> _errors = [];

    void Establish() => ConfigurePolicy(enforce: true);

    async Task Because()
    {
        Func<Task>[] operations =
        [
            () => _observers.Replay(new Replay { EventStore = "sYsTeM" }, _callContext),
            () => _observers.ReplayPartition(new ReplayPartition { EventStore = "System" }, _callContext),
            () => _observers.FastForward(new FastForward { EventStore = "System" }, _callContext),
            () => _observers.RetryPartition(new RetryPartition { EventStore = "System" }, _callContext),
            () => _observers.RetryFailedPartitions(new RetryFailedPartitions { EventStore = "System" }, _callContext),
            () => _observers.ClearPartitionQuarantine(new ClearPartitionQuarantine { EventStore = "System" }, _callContext),
            () => _observers.ClearObserverQuarantine(new ClearObserverQuarantine { EventStore = "System" }, _callContext),
            () => _observers.ClearFailedPartitions(new ClearFailedPartitions { EventStore = "System" }, _callContext)
        ];
        foreach (var operation in operations) _errors.Add(await Catch.Exception(operation));
    }

    [Fact] void should_deny_every_operational_rpc() => _errors.Count.ShouldEqual(8);
    [Fact] void should_report_permission_denied_for_every_rpc() => _errors.TrueForAll(error => error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_dispatch_to_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_log_an_audit_only_allowance() => _logger.Entries.ShouldBeEmpty();
}
