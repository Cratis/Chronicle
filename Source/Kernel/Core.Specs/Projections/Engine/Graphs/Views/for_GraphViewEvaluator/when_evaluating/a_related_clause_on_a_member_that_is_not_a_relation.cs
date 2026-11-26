// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewEvaluator.when_evaluating;

public class a_related_clause_on_a_member_that_is_not_a_relation : given.a_seeded_organization
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() =>
        GraphViewEvaluator.Evaluate(_sink, _graph, _views, new("PersonWithGroups", [new GraphViewRelatedClause("name", [])])));

    [Fact] void should_refuse_the_query() => _exception.ShouldNotBeNull();
    [Fact] void should_name_the_member() => ((InvalidGraphViews)_exception).Issues.Single().Member.ShouldEqual("name");
}
