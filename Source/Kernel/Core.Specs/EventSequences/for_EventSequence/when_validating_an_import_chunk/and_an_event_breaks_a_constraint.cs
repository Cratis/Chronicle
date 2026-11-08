// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

public class and_an_event_breaks_a_constraint : given.an_event_sequence_with_imports
{
    void Establish() => _rejectedSources.Add(_events[2].EventSourceId);

    async Task Because() => _validation = await _eventSequence.ValidateImportChunk(_importId, true, ImportEvents());

    [Fact] void should_not_ask_to_restart() => _validation.MustRestart.ShouldBeFalse();
    [Fact] void should_reject_the_event_by_its_index() => _validation.Rejections.Single().Index.ShouldEqual(2L);
    [Fact] void should_write_nothing() => _writes.ShouldEqual(0);
}
