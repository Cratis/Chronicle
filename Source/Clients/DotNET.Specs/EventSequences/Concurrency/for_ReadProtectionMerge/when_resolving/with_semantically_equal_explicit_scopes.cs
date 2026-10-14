// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_semantically_equal_explicit_scopes : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "first");
        Add(ReadProtectionContributionKind.Explicit, new(90, _label, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, []), "second");
    }

    void Because() => _plan = Merge();

    [Fact] void should_deduplicate_null_and_empty_types_and_wildcard_dimensions() => _plan.Scopes.Count.ShouldEqual(1);
}
