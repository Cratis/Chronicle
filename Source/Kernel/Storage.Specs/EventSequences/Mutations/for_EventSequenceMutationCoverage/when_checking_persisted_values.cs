// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationCoverage;

public class when_checking_persisted_values : Specification
{
    [Fact] void should_persist_untracked_as_zero() => ((int)EventSequenceMutationCoverage.Untracked).ShouldEqual(0);
    [Fact] void should_persist_unsealed_as_one() => ((int)EventSequenceMutationCoverage.Unsealed).ShouldEqual(1);
    [Fact] void should_persist_sealed_as_two() => ((int)EventSequenceMutationCoverage.Sealed).ShouldEqual(2);
}
