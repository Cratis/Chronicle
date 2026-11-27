// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_provenance;

public class for_a_named_graph : Specification
{
    static readonly SinkName _target = "graph-target";
    readonly ReadModelDefinition _model = new(
        "Graph",
        "Graph",
        "Graph",
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new(SinkConfigurationId.None, WellKnownSinkTypes.Neo4j) { Name = _target },
        new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
        []);
    ISinkProvenanceStorage _expected;
    ISinkProvenanceStorage _actual;
    Sinks _sinks;

    void Establish()
    {
        _expected = Substitute.For<ISinkProvenanceStorage>();
        var factory = Substitute.For<IGraphSinkFactory>();
        factory.TypeId.Returns(WellKnownSinkTypes.Neo4j);
        factory.GetProvenance("store", "ns", _target, _model.ContainerName).Returns(_expected);
        var configurations = Substitute.For<ISinkConfigurations>();
        configurations.ResolveFor(_model, true).Returns(new ResolvedSink(WellKnownSinkTypes.Neo4j, _target));
        configurations.ResolveFor(_model, false).Returns(new ResolvedSink(WellKnownSinkTypes.Neo4j, SinkName.Default));
        _sinks = new("store", "ns", new KnownInstancesOf<ISinkFactory>([factory]), configurations);
    }

    void Because() => _actual = _sinks.GetProvenance(_model);

    [Fact] void should_use_the_graph_payload_target() => _actual.ShouldEqual(_expected);
}
