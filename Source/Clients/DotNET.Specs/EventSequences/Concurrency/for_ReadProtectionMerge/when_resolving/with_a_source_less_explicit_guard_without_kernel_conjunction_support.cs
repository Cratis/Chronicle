// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_source_less_explicit_guard_without_kernel_conjunction_support : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90), "global guard");
    }

    void Because() => _plan = Merge(supportsConjunctions: false);

    [Fact] void should_keep_the_explicit_guard() => _plan.Scopes.Count.ShouldEqual(1);
    [Fact] void should_keep_the_explicit_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((Events.EventSequenceNumber)90u);
    [Fact] void should_keep_it_source_less() => _plan.Scopes[0].Value.EventSourceId.ShouldBeNull();
    [Fact] void should_put_it_under_the_label() => _plan.Scopes[0].Key.ShouldEqual(_label);
}
