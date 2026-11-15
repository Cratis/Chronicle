// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_graph_for;

public class and_the_graph_names_a_sink : Specification
{
    static readonly SinkTypeId _type = "Neo4j";

    IGraphSinkFactory _factory;
    IGraphSink _people;
    IGraphSink _organization;
    IGraphSink _peopleResult;
    IGraphSink _organizationResult;
    ISinkConfigurations _configurations;
    Sinks _sinks;

    void Establish()
    {
        _people = Substitute.For<IGraphSink>();
        _organization = Substitute.For<IGraphSink>();
        _factory = (IGraphSinkFactory)Substitute.For([typeof(ISinkFactory), typeof(IGraphSinkFactory)], []);
        _factory.TypeId.Returns(_type);
        _factory.CreateGraphFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>(), new SinkName("people")).Returns(_people);
        _factory.CreateGraphFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>(), new SinkName("organization")).Returns(_organization);
        _configurations = Substitute.For<ISinkConfigurations>();
        _configurations.ResolveFor(Arg.Any<ReadModelDefinition>(), true).Returns(call => new ResolvedSink(_type, call.Arg<ReadModelDefinition>().Sink.Name));
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]), _configurations);
    }

    async Task Because()
    {
        _peopleResult = await _sinks.GetGraphFor(Graph("People", "people"));
        _organizationResult = await _sinks.GetGraphFor(Graph("Organization", "organization"));
    }

    [Fact] void should_serve_the_first_graph_from_its_sink() => _peopleResult.ShouldEqual(_people);
    [Fact] void should_serve_the_second_graph_from_its_sink() => _organizationResult.ShouldEqual(_organization);
    [Fact] void should_resolve_as_a_graph() => _configurations.Received().ResolveFor(Arg.Any<ReadModelDefinition>(), true);

    static ReadModelDefinition Graph(string name, SinkName sink) => new(
        name,
        name,
        name,
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new SinkDefinition(SinkConfigurationId.None, _type) { Name = sink },
        new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema { Description = name } },
        []);
}
