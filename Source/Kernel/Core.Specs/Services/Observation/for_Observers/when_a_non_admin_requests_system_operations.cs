// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security.given;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_a_non_admin_requests_system_operations : given.all_dependencies
{
    public static TheoryData<string> Callers() => a_system_store_caller.NonAdministrators();

    [Theory]
    [MemberData(nameof(Callers))]
    public async Task should_refuse_every_operation_and_query_without_reaching_a_grain(string caller)
    {
        var probe = new Probe();
        var context = CallFor(probe, CallerNamed(caller));
        Func<Task>[] calls =
        [
            () => _observers.Replay(new Replay { EventStore = "sYsTeM" }, context),
            () => _observers.ReplayPartition(new ReplayPartition { EventStore = "System" }, context),
            () => _observers.FastForward(new FastForward { EventStore = "SYSTEM" }, context),
            () => _observers.RetryPartition(new RetryPartition { EventStore = "System" }, context),
            () => _observers.RetryFailedPartitions(new RetryFailedPartitions { EventStore = "System" }, context),
            () => _observers.ClearPartitionQuarantine(new ClearPartitionQuarantine { EventStore = "System" }, context),
            () => _observers.ClearObserverQuarantine(new ClearObserverQuarantine { EventStore = "System" }, context),
            () => _observers.ClearFailedPartitions(new ClearFailedPartitions { EventStore = "System" }, context),
            () => _observers.WaitForCompletion(new WaitForObserverCompletionRequest { EventStore = "System" }, context),
            () => _observers.GetObserverInformation(new GetObserverInformationRequest { EventStore = "System" }, context),
            () => _observers.GetConnectedClientsForObserver(new GetConnectedClientsForObserverRequest { EventStore = "System" }, context),
            () => _observers.GetObservers(new AllObserversRequest { EventStore = "System" }, context),
            () => _observers.GetReplayableObserversForEventTypes(new GetReplayableObserversForEventTypesRequest { EventStore = "System" }, context),
            () => _observers.ObserveObservers(new AllObserversRequest { EventStore = "System" }, context).FirstAsync().ToTask(),
            () => _observers.RemoveObserver(new RemoveObserver { EventStore = "System" }, context)
        ];

        foreach (var call in calls)
        {
            var error = await Catch.Exception(call);
            (error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
        }

        _grainFactory.ReceivedCalls().ShouldBeEmpty();
        _storage.ReceivedCalls().ShouldBeEmpty();
    }
}
