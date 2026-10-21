// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_hashes_are_calculated_per_added_generation : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with { Content = _snapshot.Content.Where(_ => _.Key == EventTypeGeneration.First).ToDictionary() };
    }

    async Task Because() => await Perform();

    [Fact] void should_add_both_missing_generations() => _additions.Count.ShouldEqual(2);
    [Fact] void should_hash_each_protected_content() => _additions.TrueForAll(_ => _.Hash == new EventHashCalculator().Calculate(TypeId, _snapshot.EventSourceId, _.Content)).ShouldBeTrue();
}
