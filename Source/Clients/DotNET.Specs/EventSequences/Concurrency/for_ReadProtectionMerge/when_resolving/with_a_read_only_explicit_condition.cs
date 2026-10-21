// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_read_only_explicit_condition : given.contributions
{
    void Establish() => Add(ReadProtectionContributionKind.Explicit, new(90, _label), "stream read", ReadProtectionContributionIntent.ReadEnrolled);

    void Because() => _plan = Merge(false);

    [Fact] void should_not_turn_a_read_only_command_into_validate_only() => _plan.Scopes.ShouldBeEmpty();
}
