// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.when_matching;

public class an_added_clause : given.an_evaluator_and_changes
{
    static readonly WhenClause _clause = new(WhenClauseType.Added, []);

    [Fact] void should_match_an_added_item() => _evaluator.Matches(_clause, _addedChange).ShouldBeTrue();
    [Fact] void should_not_match_a_removed_item() => _evaluator.Matches(_clause, _removedChange).ShouldBeFalse();
    [Fact] void should_not_match_a_modified_item() => _evaluator.Matches(_clause, _modifiedChange).ShouldBeFalse();
}
