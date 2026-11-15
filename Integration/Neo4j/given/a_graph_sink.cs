// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Neo4j;
using Cratis.Chronicle.Storage.Neo4j.Integration;

namespace Cratis.Chronicle.Neo4j.Integration.given;

/// <summary>
/// A sink for the organization graph, in the namespace of the specification, whose schema is in place on the real server.
/// </summary>
/// <param name="fixture">The <see cref="Neo4jFixture"/> providing the Neo4j server.</param>
public class a_graph_sink(Neo4jFixture fixture) : a_graph(fixture)
{
    protected Neo4jGraphSink _sink = default!;
    protected GraphScenarioRun _run = default!;

    async Task Establish()
    {
        _sink = CreateSink();
        await _sink.EnsureSchema();
        _run = new(_sink);
    }
}
