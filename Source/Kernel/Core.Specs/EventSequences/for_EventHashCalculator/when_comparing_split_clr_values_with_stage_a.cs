// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_comparing_split_clr_values_with_stage_a : Specification
{
    given.canonical_comparison[] _comparisons;

    void Because() => _comparisons = given.extended_event_corpus.SplitShapes()
        .Concat(given.direct_value_corpus.Shapes())
        .Select(shape => given.canonical_comparison.For(shape.Name, shape.Content, shape.Direct))
        .ToArray();

    [Fact] void should_exercise_direct_events() => _comparisons.Any(comparison => comparison.ExpectedDirect).ShouldBeTrue();
    [Fact] void should_exercise_fallback_events() => _comparisons.Any(comparison => !comparison.ExpectedDirect).ShouldBeTrue();
    [Fact] void should_select_the_expected_path_for_every_event() => _comparisons.Where(comparison => comparison.Direct != comparison.ExpectedDirect).Select(comparison => comparison.Name).ShouldBeEmpty();
    [Fact] void should_match_every_stage_a_byte() => _comparisons.Where(comparison => !comparison.BytesEqual).Select(comparison => comparison.Name).ShouldBeEmpty();
    [Fact] void should_match_every_stage_a_hash() => _comparisons.Where(comparison => !comparison.HashEqual).Select(comparison => comparison.Name).ShouldBeEmpty();
}
