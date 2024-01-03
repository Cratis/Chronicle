// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties;

public class counting_events : ProjectionSpecificationFor<Model>
{
    string event_source_id;
    ProjectionResult<Model> result;
    EventWithPropertiesForAllSupportedTypes first_event_appended;
    EventWithPropertiesForAllSupportedTypes second_event_appended;
    protected override IProjectionFor<Model> CreateProjection() => new CountingEventsProjection();

    async Task Because()
    {
        event_source_id = Guid.NewGuid().ToString();

        first_event_appended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
        second_event_appended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
        await context.EventLog.Append(event_source_id, first_event_appended);
        await context.EventLog.Append(event_source_id, second_event_appended);
        result = await context.GetById(event_source_id);
    }

    [Fact] void should_hold_correct_count() => result.Model.IntValue.ShouldEqual(2);
}
