// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties_from_every.with_event_of_first_event_type_applied.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_properties_from_every;

[Collection(ChronicleCollection.Name)]
public class with_event_of_first_event_type_applied(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<FromEveryProjection, ReadModel>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmptyEvent), typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            EventsToAppend.Add(new EmptyEvent());
        }
    }

    [Fact] void should_set_the_last_updated_property_to_occurred_for_event() => Context.Result.LastUpdated.ShouldEqual(Context.AppendedEvents[0].Context.Occurred);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
