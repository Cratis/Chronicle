// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unrelated_observer_is_behind : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                "observer-b",
                [new Concepts.Events.EventType("b-recorded", 1)],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Client,
                true)
        ]);
        _observerStateStorage.GetAll().Returns(
        [
            new ObserverState { Identifier = "observer-b", LastHandledEventSequenceNumber = 12UL }
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
        {
            EventStore = "event-store",
            Namespace = "event-store-namespace",
            EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
            TailEventSequenceNumber = 42UL,
            EventTypes = [new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }]
        },
        new CallContext(new CallOptions(cancellationToken: timeout.Token)));
    }

    [Fact] void should_complete_without_waiting_for_the_unrelated_observer() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_query_failed_partitions() => _failedPartitionsStorage.DidNotReceive().GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>());
}
