// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_QueryableGraph.when_querying;

public class with_a_property_clause_on_a_relation : given.a_queryable_graph
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _graph.Nodes<GroupWithParent>().Where(_ => _.Parent, null));

    [Fact] void should_refuse_the_clause() => _error.ShouldBeOfExactType<GraphViewMemberIsNotAProperty>();
}
