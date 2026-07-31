// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.when_matching;

public class a_logical_or_clause : given.an_evaluator_and_changes
{
    [Fact] void should_match_when_any_property_changed() => _evaluator.Matches(new(WhenClauseType.LogicalOr, ["age", "email"]), _modifiedChange).ShouldBeTrue();
    [Fact] void should_not_match_when_no_property_changed() => _evaluator.Matches(new(WhenClauseType.LogicalOr, ["age"]), _modifiedChange).ShouldBeFalse();
}
