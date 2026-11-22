// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Projections.ReadModels;
using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_reading_projection_state_concurrently.and_many_requests_arrive.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_reading_projection_state_concurrently;

[Collection(ChronicleCollection.Name)]
public class and_many_requests_arrive(context context) : Given<context>(context)
{
    const int Requests = 64;

    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<CountingEventsProjection, ReadModel>(chronicleFixture)
    {
        public ProjectionState[] States;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
        }

        async Task Because()
        {
            var startReads = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reads = Enumerable.Range(0, Requests).Select(_ => ReadStateAfter(startReads.Task)).ToArray();
            startReads.SetResult();
            States = await Task.WhenAll(reads).WaitAsync(TimeSpanFactory.FromSeconds(30));
        }

        async Task<ProjectionState> ReadStateAfter(Task start)
        {
            await start;
            return await EventStore.Projections.GetStateForModel<ReadModel>();
        }
    }

    [Fact] void should_complete_every_request() => Context.States.Length.ShouldEqual(Requests);
    [Fact] void should_return_the_same_state_for_every_request() => Context.States.All(state => state == Context.States[0]).ShouldBeTrue();
    [Fact] void should_report_an_active_projection() => Context.States.All(state => state.RunningState == ObserverRunningState.Active).ShouldBeTrue();
    [Fact] void should_report_an_established_subscription() => Context.States.All(state => state.IsSubscribed).ShouldBeTrue();
    [Fact] void should_report_the_last_appended_event_as_handled() => Context.States.All(state => state.LastHandledEventSequenceNumber == Context.LastEventSequenceNumber).ShouldBeTrue();
    [Fact] void should_report_the_next_sequence_number_after_the_handled_event() => Context.States.All(state => state.NextEventSequenceNumber == Context.LastEventSequenceNumber + 1).ShouldBeTrue();
}
