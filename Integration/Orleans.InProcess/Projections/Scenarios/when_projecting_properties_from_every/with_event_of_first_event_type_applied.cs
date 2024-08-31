// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties_from_every.with_event_of_first_event_type_applied.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties_from_every;

[Collection(GlobalCollection.Name)]
public class with_event_of_first_event_type_applied(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<FromEveryProjection, Model>(globalFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmptyEvent), typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            EventsToAppend.Add(new EmptyEvent());
        }
    }

    [Fact] void should_set_the_last_updated_property_to_occurred_for_event() => Context.Result.LastUpdated.ShouldEqual(Context.AppendedEvents[0].Context.Occurred);
}
