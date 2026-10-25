// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_default_event_source_id_tracing : given.an_event_sequence_with_identifier_tracing
{
    async Task Because() => await Append();

    [Fact] void should_include_the_raw_id() => _append.GetTagItem("event_source_id").ShouldEqual(SourceId);
    [Fact] void should_send_the_original_id_to_the_kernel() => _request.EventSourceId.ShouldEqual(SourceId);
    [Fact] void should_never_generate_the_raw_id_tag() => _startedTags.Any(_ => _.Key == "event_source_id").ShouldBeFalse();
    [Fact] void should_not_start_with_a_raw_identifier() => _startedTags.Any(_ => Equals(_.Value, SourceId)).ShouldBeFalse();
}
