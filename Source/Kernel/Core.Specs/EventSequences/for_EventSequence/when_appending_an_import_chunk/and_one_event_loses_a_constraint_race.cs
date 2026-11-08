// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_one_event_loses_a_constraint_race : given.an_event_sequence_with_imports
{
    void Establish() => _rejectedSources.Add(_events[1].EventSourceId);

    async Task Because() => _chunkResult = await AppendChunk();

    [Fact] void should_settle() => _chunkResult.IsSettled.ShouldBeTrue();
    [Fact] void should_report_the_event_that_lost_as_rejected() => _chunkResult.Outcomes.Single(_ => _.Index == 1).Kind.ShouldEqual(ImportEventOutcomeKind.Rejected);
    [Fact] void should_give_the_reason() => _chunkResult.Outcomes.Single(_ => _.Index == 1).Reasons.ShouldNotBeEmpty();
    [Fact] void should_commit_the_rest() => _chunkResult.Outcomes.Count(_ => _.Kind == ImportEventOutcomeKind.Appended).ShouldEqual(2);
    [Fact] void should_write_only_the_valid_events() => _written.Select(_ => _.EventSourceId).ShouldContainOnly(_events[0].EventSourceId, _events[2].EventSourceId);
}
