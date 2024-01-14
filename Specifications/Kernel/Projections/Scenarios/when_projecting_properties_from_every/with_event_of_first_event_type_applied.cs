// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties_from_every;

public class with_event_of_first_event_type_applied : ProjectionSpecificationFor<Model>
{
    string event_source_id;
    protected override IProjectionFor<Model> CreateProjection() => new FromEveryProjection();
    ProjectionResult<Model> result;

    async Task Because()
    {
        event_source_id = Guid.NewGuid().ToString();
        await context.EventLog.Append(event_source_id, new EmptyEvent());
        result = await context.GetById(event_source_id);
    }

    [Fact] void should_set_the_last_updated_property_to_occurred_for_event() => result.Model.LastUpdated.ShouldEqual(context.AppendedEvents.First().Context.Occurred);
}
