// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_from_event_that_adds_child;


public class without_keys_and_identified_by_specified : ProjectionSpecificationFor<ModelWithChildren>
{
    string model_id;
    ProjectionResult<ModelWithChildren> result;
    protected override IProjectionFor<ModelWithChildren> CreateProjection() => new ChildrenWithoutKeysProjection();

    async Task Because()
    {
        model_id = Guid.NewGuid().ToString();
        await context.EventLog.Append(model_id, EventWithChildObject.Create());
        await context.EventLog.Append(model_id, EventWithChildObject.Create());
        result = await context.GetById(model_id);
    }

    [Fact] void should_have_two_children() => result.Model.Children.Count().ShouldEqual(2);
}
