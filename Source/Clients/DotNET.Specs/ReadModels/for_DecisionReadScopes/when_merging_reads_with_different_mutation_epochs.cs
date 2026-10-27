// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadScopes;

public class when_merging_reads_with_different_mutation_epochs : Specification
{
    ConcurrencyScope _result;
    ConcurrencyScope _onlyOne;

    void Because()
    {
        var types = new[] { new EventType("created", EventTypeGeneration.First) };
        _result = DecisionReadScopes.Merge(
            new((EventSequenceNumber)8, "source", EventTypes: types) { MutationEpoch = 6UL },
            new((EventSequenceNumber)4, "source", EventTypes: types) { MutationEpoch = 4UL });
        _onlyOne = DecisionReadScopes.Merge(
            new((EventSequenceNumber)8, "source", EventTypes: types),
            new((EventSequenceNumber)4, "source", EventTypes: types) { MutationEpoch = 4UL });
    }

    [Fact] void should_keep_the_oldest_epoch() => _result.MutationEpoch.ShouldEqual(4UL);
    [Fact] void should_keep_the_only_epoch_when_the_other_read_has_none() => _onlyOne.MutationEpoch.ShouldEqual(4UL);
}
