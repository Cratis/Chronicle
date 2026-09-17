// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant.given;

public class a_model_bound_projection_builder_for_variants : Specification
{
    internal ModelBoundProjectionBuilder builder;
    protected IEventTypes event_types;

    void Establish()
    {
        var naming_policy = new TestNamingPolicy();
        event_types = new EventTypesForSpecifications([
            typeof(IssueCreated),
            typeof(IssueStarted),
            typeof(PullRequestCreated),
            typeof(BuildCompleted)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }
}
