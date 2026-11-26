// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_outbox_with_a_subset : given.a_mixed_catalog
{
    void Establish() => _criteria = _criteria with
    {
        EventTypes = [new Concepts.Events.EventType("public-two", new EventTypeGeneration(3)), new Concepts.Events.EventType("private", EventTypeGeneration.First)],
        EventSourceId = "source"
    };

    async Task Because() => await Narrow("outbox");

    [Fact] void should_intersect_with_the_requested_types() => Ids(_result).ShouldContainOnly("public-two");
    [Fact] void should_keep_the_requested_generation() => _result!.EventTypes!.Single().Generation.ShouldEqual(new EventTypeGeneration(3));
    [Fact] void should_keep_the_other_criteria() => _result!.EventSourceId.ShouldEqual((EventSourceId)"source");
}
