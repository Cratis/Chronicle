// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_reserved_user_label : given.contributions
{
    void Establish() => _contributions.Add(new("$read-protection/routing/user/0", ReadProtectionContributionKind.Explicit, new(90), _target, "user condition"));

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_colliding_with_internal_labels() => _error.ShouldBeOfExactType<Transactions.ReservedConcurrencyScopeLabel>();
}
