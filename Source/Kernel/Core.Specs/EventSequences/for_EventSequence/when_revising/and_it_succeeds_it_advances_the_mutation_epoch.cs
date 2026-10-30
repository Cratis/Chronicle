// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_it_succeeds_it_advances_the_mutation_epoch : a_revisable_sequence
{
    EventSequenceMutationEpoch[] _written;

    async Task Because()
    {
        await ReviseFirstEvent();
        _written = [.. _snapshots.Snapshots.Skip(_stateWritesBeforeRevise).Select(_ => _.MutationEpoch)];
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_mark_the_mutation_before_it_starts() => _written[^2].Value.ShouldEqual(1UL);
    [Fact] void should_settle_the_epoch_after_it() => _written[^1].Value.ShouldEqual(2UL);
}
