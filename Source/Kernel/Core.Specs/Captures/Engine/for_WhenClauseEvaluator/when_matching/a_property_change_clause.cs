// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.when_matching;

public class a_property_change_clause : given.an_evaluator_and_changes
{
    [Fact] void should_match_a_changed_property() => _evaluator.Matches(new(WhenClauseType.PropertyChange, ["email"]), _modifiedChange).ShouldBeTrue();
    [Fact] void should_not_match_an_unchanged_property() => _evaluator.Matches(new(WhenClauseType.PropertyChange, ["age"]), _modifiedChange).ShouldBeFalse();
    [Fact] void should_not_match_an_added_item() => _evaluator.Matches(new(WhenClauseType.PropertyChange, ["email"]), _addedChange).ShouldBeFalse();
}
