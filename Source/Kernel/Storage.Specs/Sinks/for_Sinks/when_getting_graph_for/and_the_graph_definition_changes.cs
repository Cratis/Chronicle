// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_graph_for;

public class and_the_graph_definition_changes : given.a_graph_sink_factory
{
    IGraphSink _result;
    IGraphSink _again;

    async Task Because()
    {
        await _sinks.GetGraphFor(_model);
        _result = await _sinks.GetGraphFor(DefinitionDescribedAs("one node and one more"));
        _again = await _sinks.GetGraphFor(DefinitionDescribedAs("one node and one more"));
    }

    [Fact] void should_serve_a_sink_that_knows_the_changed_graph() => _result.ShouldEqual(_second);
    [Fact] void should_ensure_the_schema_of_the_changed_graph() => _second.Received(1).EnsureSchema();
    [Fact] void should_keep_serving_the_new_sink_while_the_graph_stays_the_same() => _again.ShouldEqual(_second);
}
