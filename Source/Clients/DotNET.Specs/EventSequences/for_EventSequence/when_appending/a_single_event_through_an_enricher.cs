// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_single_event_through_an_enricher : given.an_event_sequence_with_a_tag_resolver
{
    async Task Because() => await _eventSequence.Append(_sourceId, "one");

    [Fact] void should_send_the_enriched_tags_on_the_tagged_rpc() => _singleRequest.NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("enriched", "one")]);
}
