// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using HandlebarsDotNet;
using Humanizer;
using Xunit.Abstractions;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.non_existent.with_multiple_partitions.and_reducer_is_registered_while_there_are_events_in_sequence.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.non_existent.with_multiple_partitions;

[Collection(GlobalCollection.Name)]
public class and_reducer_is_registered_while_there_are_events_in_sequence(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reducer_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> Events;

        public ObserverState ReducerObserverState;

        public EventSequenceNumber LastEventSequenceNumberAppended;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAppended = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            ReducerObserver = GetObserverForReducer<ReducerWithoutDelay>();
            await ReducerObserver.WaitTillSubscribed();
            await ReducerObserver.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAppended);

            await Reducer.WaitTillHandledEventReaches(Events.Count);

            ReducerObserverState = await ReducerObserver.GetState();
        }
    }

    [Fact]
    void should_have_observer_in_running_state() => Context.ReducerObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReducerObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAppended.Value);

    [Fact]
    void should_process_all_events() => Context.Reducer.HandledEvents.ShouldEqual(Context.Events.Count);
}
