// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_processing_many_read_models : given.a_pinned_projection_grain
{
    async Task Because() => await _grain.Process(EventStoreNamespaceName.Default, [_stored]);

    [Fact] void should_release_the_event_to_its_pin() => _projection.Received(1).ReleaseEvent(_pin, _stored);
    [Fact] void should_project_the_released_event() => _projected.Event.ShouldBeSame(_released);
}
