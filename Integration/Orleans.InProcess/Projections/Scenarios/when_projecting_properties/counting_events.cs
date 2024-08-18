// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties.counting_events.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties;

[Collection(GlobalCollection.Name)]
public class counting_events(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<CountingEventsProjection, Model>(globalFixture)
    {
        string event_source_id;
        EventWithPropertiesForAllSupportedTypes first_event_appended;
        EventWithPropertiesForAllSupportedTypes second_event_appended;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            event_source_id = Guid.NewGuid().ToString();

            first_event_appended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            EventsToAppend.Add(first_event_appended);
            second_event_appended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            EventsToAppend.Add(second_event_appended);
        }
    }

    [Fact] void should_hold_correct_count() => Context.Result.IntValue.ShouldEqual(2);
}
