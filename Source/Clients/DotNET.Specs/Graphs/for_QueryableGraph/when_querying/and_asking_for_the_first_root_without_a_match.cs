// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_QueryableGraph.when_querying;

public class and_asking_for_the_first_root_without_a_match : given.a_queryable_graph
{
    GroupWithParent? _result;

    async Task Because() => _result = await _graph.Nodes<GroupWithParent>().WhereRelated(_ => _.Parent).FirstOrDefault();

    [Fact] void should_ask_for_the_first_root() => _query!.Shape.ShouldEqual(GraphQueryShape.First);
    [Fact] void should_add_a_related_clause_on_the_relation() => ((GraphRelatedClause)_query!.Clauses.Single()).Member.ShouldEqual("parent");
    [Fact] void should_add_no_nested_clauses() => ((GraphRelatedClause)_query!.Clauses.Single()).Clauses.ShouldBeEmpty();
    [Fact] void should_give_null() => _result.ShouldBeNull();
}
