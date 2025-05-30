// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties_from_every.waiting_for_each_event.with_event_of_instances_of_two_event_types_applied.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties_from_every.waiting_for_each_event;

[Collection(ChronicleCollection.Name)]
public class with_event_of_instances_of_two_event_types_applied(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<FromEveryProjection, Model>(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmptyEvent), typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            WaitForEachEvent = true;
            EventsToAppend.Add(new EmptyEvent());
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
        }
    }

    [Fact] void should_set_the_last_updated_property_to_be_occurred_for_second_event() => Context.Result.LastUpdated.Ticks.ShouldEqual(Context.AppendedEvents[1].Context.Occurred.Ticks);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
