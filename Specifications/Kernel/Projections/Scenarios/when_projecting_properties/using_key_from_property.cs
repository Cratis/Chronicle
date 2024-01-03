// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties;

public class using_key_from_property : ProjectionSpecificationFor<Model>
{
    string event_source_id;
    string model_id;
    ProjectionResult<Model> result;
    protected override IProjectionFor<Model> CreateProjection() => new KeyFromPropertyProjection();

    async Task Because()
    {
        event_source_id = Guid.NewGuid().ToString();
        model_id = Guid.NewGuid().ToString();
        await context.EventLog.Append(event_source_id, EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues() with
        {
            StringValue = model_id
        });
        result = await context.GetById(event_source_id, model_id);
    }

    [Fact] void should_return_model() => result.Model.ShouldNotBeNull();
}
