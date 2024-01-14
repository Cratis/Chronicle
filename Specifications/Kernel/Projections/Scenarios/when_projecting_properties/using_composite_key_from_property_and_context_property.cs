// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Projections;
using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_properties;

public class using_composite_key_from_property_and_context_property : ProjectionSpecificationFor<ModelWithCompositeKey>
{
    string event_source_id;
    CompositeKey model_id;
    ProjectionResult<ModelWithCompositeKey> result;
    protected override IProjectionFor<ModelWithCompositeKey> CreateProjection() => new CompositeKeyFromPropertyAndContextPropertyProjection();

    async Task Because()
    {
        event_source_id = Guid.NewGuid().ToString();
        model_id = new(Guid.NewGuid().ToString(), 0);
        await context.EventLog.Append(event_source_id, EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues() with
        {
            StringValue = model_id.First
        });
        result = await context.GetById(event_source_id, model_id);
    }

    [Fact] void should_return_model() => result.Model.ShouldNotBeNull();
}
