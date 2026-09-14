// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_from_all_with_dynamic_dictionary_key.with_events_of_two_different_types_applied.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_from_all_with_dynamic_dictionary_key;

[Collection(ChronicleCollection.Name)]
public class with_events_of_two_different_types_applied(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<FromAllWithDynamicDictionaryKeyProjection, EventCountByType>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(FirstCountableEvent), typeof(SecondCountableEvent)];

        /// <summary>
        /// An observer subscribed to all events (see <see cref="FromAllWithDynamicDictionaryKeyProjection"/>) has to
        /// catch up the whole shared fixture's accumulated event log, not just its own event source, before it
        /// reaches Active - a materially larger job than the narrow, single-event-source catch-up every other
        /// projection scenario in this fixture does. Give it a wider berth than the library's default.
        /// </summary>
        protected override TimeSpan? SubscriptionTimeout => TimeSpan.FromMinutes(5);

        void Establish()
        {
            // Append 3 events of the first type
            EventsToAppend.Add(new FirstCountableEvent("First event instance 1"));
            EventsToAppend.Add(new FirstCountableEvent("First event instance 2"));
            EventsToAppend.Add(new FirstCountableEvent("First event instance 3"));

            // Append 2 events of the second type
            EventsToAppend.Add(new SecondCountableEvent("Second event instance 1"));
            EventsToAppend.Add(new SecondCountableEvent("Second event instance 2"));
        }
    }

    [Fact] void should_have_count_for_first_event_type() => Context.Result.CountsByEventTypeId.ContainsKey("0d6f8b24-0b3d-4e9f-9c4a-5e8b7f1a2c3d").ShouldBeTrue();
    [Fact] void should_have_count_for_second_event_type() => Context.Result.CountsByEventTypeId.ContainsKey("1e7f9c35-1c4e-5faf-ad5b-6f9c8a2b3d4e").ShouldBeTrue();
    [Fact] void should_have_correct_count_for_first_event_type() => Context.Result.CountsByEventTypeId["0d6f8b24-0b3d-4e9f-9c4a-5e8b7f1a2c3d"].ShouldEqual(3);
    [Fact] void should_have_correct_count_for_second_event_type() => Context.Result.CountsByEventTypeId["1e7f9c35-1c4e-5faf-ad5b-6f9c8a2b3d4e"].ShouldEqual(2);
    [Fact] void should_have_exactly_two_dictionary_keys() => Context.Result.CountsByEventTypeId.Count.ShouldEqual(2);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
