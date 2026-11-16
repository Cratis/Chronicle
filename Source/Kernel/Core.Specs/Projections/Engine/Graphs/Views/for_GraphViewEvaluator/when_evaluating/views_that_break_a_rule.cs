// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewEvaluator.when_evaluating;

public class views_that_break_a_rule : given.a_seeded_organization
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() =>
        GraphViewEvaluator.Evaluate(_sink, _graph, [new("Broken", "Nowhere", [])], new("Broken", [])));

    [Fact] void should_refuse_the_query() => _exception.ShouldNotBeNull();
    [Fact] void should_report_the_rule_that_is_broken() => ((InvalidGraphViews)_exception).Issues.Single().Reason.ShouldEqual(GraphViewReason.ViewRootNotANode);
}
