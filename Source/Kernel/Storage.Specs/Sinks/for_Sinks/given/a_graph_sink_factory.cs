// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.given;

public abstract class a_graph_sink_factory : Specification
{
    static SinkTypeId _type = "0f6cf9d1-5a3c-4b68-8d6a-8c0b7d7a6c11";

    protected Sinks _sinks;
    protected IGraphSinkFactory _factory;
    protected IGraphSink _first;
    protected IGraphSink _second;
    protected ReadModelDefinition _model;

    void Establish()
    {
        _model = DefinitionDescribedAs("one node");
        _first = Substitute.For<IGraphSink>();
        _second = Substitute.For<IGraphSink>();
        _factory = (IGraphSinkFactory)Substitute.For([typeof(ISinkFactory), typeof(IGraphSinkFactory)], []);
        _factory.TypeId.Returns(_type);
        _factory.CreateGraphFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>()).Returns(_first, _second);
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    protected static ReadModelDefinition DefinitionDescribedAs(string description) => new(
        "OrgGraph",
        "OrgGraph",
        "OrgGraph",
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new SinkDefinition(SinkConfigurationId.None, _type),
        new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema { Description = description } },
        []);
}
