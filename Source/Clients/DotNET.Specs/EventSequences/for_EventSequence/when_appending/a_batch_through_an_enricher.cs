// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_batch_through_an_enricher : given.an_event_sequence_with_a_tag_resolver
{
    EventContext[] _observedContexts;

    void Establish() => _eventSequence.AppendOperations.Subscribe(events => _observedContexts = events.Select(_ => _.Event.Context).ToArray());

    async Task Because() => await _eventSequence.AppendMany(_sourceId, ["one", "two"]);

    [Fact] void should_resolve_tags_per_event() => _batchRequest.Events.Select(_ => _.NamedTags.Single().Value).ShouldEqual(["one", "two"]);
    [Fact] void should_notify_each_event_with_its_own_tags() => _observedContexts.Select(_ => _.NamedTags.Single().Value).ShouldEqual(["one", "two"]);
}
