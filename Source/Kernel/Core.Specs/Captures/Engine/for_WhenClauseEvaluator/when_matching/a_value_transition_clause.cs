// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.when_matching;

public class a_value_transition_clause : given.an_evaluator_and_changes
{
    [Fact] void should_match_the_expected_transition() => _evaluator.Matches(new(WhenClauseType.ValueTransition, ["status"], "active", "inactive"), _modifiedChange).ShouldBeTrue();
    [Fact] void should_not_match_a_different_transition() => _evaluator.Matches(new(WhenClauseType.ValueTransition, ["status"], "active", "archived"), _modifiedChange).ShouldBeFalse();
    [Fact] void should_not_match_an_added_item() => _evaluator.Matches(new(WhenClauseType.ValueTransition, ["status"], "active", "inactive"), _addedChange).ShouldBeFalse();
}
