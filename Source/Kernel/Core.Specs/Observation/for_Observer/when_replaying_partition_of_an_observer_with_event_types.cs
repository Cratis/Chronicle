// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_of_an_observer_with_event_types : given.an_observer
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly Key _partition = "some-partition";

    void Establish() => _definitionStorage.State = _definitionStorage.State with { EventTypes = [_eventType] };

    Task Because() => _observer.ReplayPartition(_partition);

    [Fact] void should_start_the_job_with_an_explicit_scope() => _jobsManager.Received(1).Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
        Arg.Is<ReplayObserverPartitionRequest>(_ => !_.ReaderScope!.IsWildcard && _.ReaderScope.EventTypes.SequenceEqual(new[] { _eventType })));
}
