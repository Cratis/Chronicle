// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_resolving_events_for_a_key : given.a_pinned_projection_grain
{
    IEnumerable<AppendedEvent> _events;

    async Task Because() => _events = await _grain.GetEventsForKey(EventStoreNamespaceName.Default, "the-key", [_stored]);

    [Fact] void should_resolve_the_key_from_the_released_event() => _resolved.ShouldBeSame(_released);
    [Fact] void should_return_the_original_stored_event() => _events.Single().ShouldBeSame(_stored);
}
