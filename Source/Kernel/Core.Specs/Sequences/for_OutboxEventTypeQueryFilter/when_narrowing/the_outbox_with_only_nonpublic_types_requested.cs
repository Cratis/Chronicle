// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_outbox_with_only_nonpublic_types_requested : given.a_mixed_catalog
{
    void Establish() => _criteria = _criteria with { EventTypes = [new Concepts.Events.EventType("private", EventTypeGeneration.First), new Concepts.Events.EventType("unspecified", EventTypeGeneration.First)] };

    async Task Because() => await Narrow("outbox");

    [Fact] void should_match_nothing() => _result.ShouldBeNull();
}
