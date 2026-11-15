// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_QueryableGraph.when_querying;

public class a_type_that_is_not_a_view : given.a_queryable_graph
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _graph.Nodes<Person>());

    [Fact] void should_refuse_the_type() => _error.ShouldBeOfExactType<NotAGraphView>();
}
