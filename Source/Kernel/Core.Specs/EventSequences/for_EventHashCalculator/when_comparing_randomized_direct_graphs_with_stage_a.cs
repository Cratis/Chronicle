// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_comparing_randomized_direct_graphs_with_stage_a : Specification
{
    given.canonical_comparison[] _comparisons;

    void Because() => _comparisons = given.random_canonical_graphs.Generate()
        .Select(shape => given.canonical_comparison.For(shape.Name, shape.Content, expectedDirect: true))
        .ToArray();

    [Fact] void should_exercise_generated_events() => (_comparisons.Length > 0).ShouldBeTrue();
    [Fact] void should_embed_every_eligible_scalar_in_eight_generated_graphs() => _comparisons.Length.ShouldEqual(given.direct_value_corpus.Scalars().Count() * 8);
    [Fact] void should_use_the_direct_path_for_every_graph() => _comparisons.Where(comparison => !comparison.Direct).Select(comparison => comparison.Name).ShouldBeEmpty();
    [Fact] void should_match_every_stage_a_byte() => _comparisons.Where(comparison => !comparison.BytesEqual).Select(comparison => comparison.Name).ShouldBeEmpty();
    [Fact] void should_match_every_stage_a_hash() => _comparisons.Where(comparison => !comparison.HashEqual).Select(comparison => comparison.Name).ShouldBeEmpty();
}
