// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties.counting_events.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class counting_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture ChronicleFixture) : given.a_projection_and_events_appended_to_it<CountingEventsProjection, Model>(ChronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues());
        }
    }

    [Fact] void should_hold_correct_count() => Context.Result.IntValue.ShouldEqual(2);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
