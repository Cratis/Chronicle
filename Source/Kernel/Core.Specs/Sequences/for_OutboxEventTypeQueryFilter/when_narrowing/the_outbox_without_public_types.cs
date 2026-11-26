// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_outbox_without_public_types : given.a_mixed_catalog
{
    void Establish() => _eventStore.EventTypes.GetLatestForAllEventTypes().Returns([Registration("private", EventTypeVisibility.Private)]);

    async Task Because() => await Narrow("outbox");

    [Fact] void should_match_nothing() => _result.ShouldBeNull();
}
