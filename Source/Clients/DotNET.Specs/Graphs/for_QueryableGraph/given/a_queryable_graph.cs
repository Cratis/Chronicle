// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_QueryableGraph.given;

public class a_queryable_graph : a_graph_view_reader
{
    internal GraphViews _views;
    internal IGraphQueryExecutor _executor;
    internal QueryableGraph _graph;
    internal GraphQuery? _query;

    void Establish()
    {
        _views = Read(OrganizationSchema, OrganizationViews);
        _executor = Substitute.For<IGraphQueryExecutor>();
        _executor.Execute(Arg.Do<GraphQuery>(_ => _query = _), Arg.Any<CancellationToken>()).Returns([]);
        _graph = new(_views, _executor);
    }
}
