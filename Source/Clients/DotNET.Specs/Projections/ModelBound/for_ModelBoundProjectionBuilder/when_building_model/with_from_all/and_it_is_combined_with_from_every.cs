// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_all;

public class and_it_is_combined_with_from_every : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(ModelWithBoth));

    [Fact] void should_subscribe_to_all_event_types() => _result.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_keep_the_from_all_mapping() => _result.All.Properties.Keys.ShouldContain(nameof(ModelWithBoth.LastUpdatedAt));
    [Fact] void should_keep_the_from_every_mapping() => _result.All.Properties.Keys.ShouldContain(nameof(ModelWithBoth.LastObservedAt));

    public record ModelWithBoth
    {
        [FromAll(contextProperty: nameof(EventContext.Occurred))]
        public DateTimeOffset? LastUpdatedAt { get; init; }

        [FromEvery(contextProperty: nameof(EventContext.Occurred))]
        public DateTimeOffset? LastObservedAt { get; init; }
    }
}
