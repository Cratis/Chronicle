// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_scoped_unique_history_contains_unrelated_events : for_ReindexConstraintsStep.given.a_filtered_and_full_scan_reindex
{
    void Establish()
    {
        _unique = _unique with { Scope = new(EventSourceType: EventSourceType.Default) };
        _scope = _unique.Scope.BuildScopeKey(EventSourceType.Default, "transactions", "month");
    }

    async Task Because() => await CompareScans();

    [Fact] void should_match_the_full_scan() => _filtered.ShouldEqual(_full);
    [Fact] void should_keep_the_last_claim() => _filtered.Length.ShouldEqual(1);
}
