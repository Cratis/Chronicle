// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_events_for_event_sources_through_an_enricher : given.an_event_sequence_with_a_tag_resolver
{
    async Task Because() => await _eventSequence.AppendMany([new EventForEventSourceId(_sourceId, "one"), new EventForEventSourceId(EventSourceId.New(), "two") { NamedTags = [new(new TagName("own"), "tag")] }]);

    [Fact] void should_resolve_tags_per_event() => _routedRequest.Events.Select(_ => string.Join(',', _.NamedTags.Select(tag => $"{tag.Name}={tag.Value}"))).ShouldEqual(["enriched=one", "own=tag,enriched=two"]);
}
