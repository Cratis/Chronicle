// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_cross_target_explicit_without_reads : given.contributions
{
    void Establish() => _contributions.Add(new(_label, ReadProtectionContributionKind.Explicit, new(90, _label), new("store", "other-tenant", EventSequenceId.Log), "other tenant version"));

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_check_targets_before_the_no_read_rule() => _error.ShouldBeOfExactType<Transactions.ReadDependencyTargetMismatch>();
}
