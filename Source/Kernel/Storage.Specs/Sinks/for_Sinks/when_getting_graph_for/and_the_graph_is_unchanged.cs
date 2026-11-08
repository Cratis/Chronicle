// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_graph_for;

public class and_the_graph_is_unchanged : given.a_graph_sink_factory
{
    IGraphSink _result;

    async Task Because()
    {
        await _sinks.GetGraphFor(_model);
        _result = await _sinks.GetGraphFor(DefinitionDescribedAs("one node"));
    }

    [Fact] void should_serve_the_sink_it_created() => _result.ShouldEqual(_first);
    [Fact] void should_create_the_sink_once() => _factory.Received(1).CreateGraphFor(string.Empty, string.Empty, Arg.Any<Concepts.ReadModels.ReadModelDefinition>());
    [Fact] void should_ensure_the_schema_once() => _first.Received(1).EnsureSchema();
}
