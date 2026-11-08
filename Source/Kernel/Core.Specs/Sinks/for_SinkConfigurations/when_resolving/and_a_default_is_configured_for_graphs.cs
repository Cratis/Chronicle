// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_a_default_is_configured_for_graphs : given.sink_configurations
{
    ResolvedSink _graph;
    ResolvedSink _readModel;

    protected override SinksOptions Sinks => new() { Graphs = "people", Configurations = base.Sinks.Configurations };

    void Because()
    {
        _graph = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.Neo4j), graph: true);
        _readModel = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.MongoDB), graph: false);
    }

    [Fact] void should_store_graphs_to_the_default() => _graph.Name.ShouldEqual(new SinkName("People"));
    [Fact] void should_leave_read_models_on_their_implicit_default() => _readModel.IsImplicitDefault.ShouldBeTrue();
}
