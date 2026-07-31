// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.when_matching;

public class an_expression_clause : given.an_evaluator_and_changes
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _evaluator.Matches(new(WhenClauseType.Expression, [], Expression: "item.age > 40"), _modifiedChange));

    [Fact] void should_throw_unsupported_capture_capability() => _error.ShouldBeOfExactType<UnsupportedCaptureCapability>();
}
