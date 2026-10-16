// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_the_commit_result_is_unknown_and_the_slots_are_empty : given.an_append_with_settlement
{
    void Establish() => _empty = true;
    async Task Because() => await AppendBatch();
    [Fact] void should_not_throw_a_failure() => _error.ShouldBeNull();
    [Fact] void should_report_unknown() => _batchResult.Errors.ShouldContain((AppendError)"AppendOutcomeUnknown");
    [Fact] void should_probe_the_slots() => _probes.ShouldEqual(1);
}
