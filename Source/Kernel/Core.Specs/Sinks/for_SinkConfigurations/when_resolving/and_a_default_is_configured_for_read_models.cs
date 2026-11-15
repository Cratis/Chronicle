// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_a_default_is_configured_for_read_models : given.sink_configurations
{
    ResolvedSink _readModel;
    ResolvedSink _graph;

    protected override SinksOptions Sinks => new() { ReadModels = "reporting", Configurations = base.Sinks.Configurations };

    void Because()
    {
        _readModel = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.MongoDB), graph: false);
        _graph = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.Neo4j), graph: true);
    }

    [Fact] void should_store_read_models_to_the_default() => _readModel.Name.ShouldEqual(new SinkName("Reporting"));
    [Fact] void should_use_the_type_of_the_default() => _readModel.Type.ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_leave_graphs_on_their_implicit_default() => _graph.IsImplicitDefault.ShouldBeTrue();
}
