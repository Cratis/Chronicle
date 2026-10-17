// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_a_legacy_request_differs_from_the_stored_definition : given.a_catch_up_observer_job
{
    static readonly EventType _storedType = new("current-type", EventTypeGeneration.First);
    static readonly Key _key = (Key)"current-type-partition";

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor, EventTypes = [new("legacy-type", EventTypeGeneration.First)] };
        _eventStoreStorage.Observers.Get(_request.ObserverKey.ObserverId)
            .Returns(ObserverDefinition.Empty with { EventTypes = [_storedType] });
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_enumerate_with_the_current_definition_types() => _keyIndexes.Received(1).GetFor(_request.ObserverKey, Arg.Is<ObserverReaderScope>(_ => !_.IsWildcard && _.EventTypes.SequenceEqual(new[] { _storedType })));
    [Fact] void should_register_the_enumerated_partition() => _observer.Received(1).RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.SequenceEqual(new[] { _key })));
    [Fact] void should_leave_legacy_step_type_resolution_unchanged() => ((HandleEventsForPartitionArguments)_job.PreparedSteps![0].Request).ReaderScope.ShouldBeNull();
}
