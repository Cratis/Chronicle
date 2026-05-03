// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_known_type;

public class and_sink_does_not_exist_yet : Specification
{
    static SinkTypeId _type = "df371e5d-b244-48d0-aaad-f298a127dd92";

    Sinks _sinks;
    ISinkFactory _factory;
    ISink _sink;
    ISink _result;
    ReadModelDefinition _model;

    void Establish()
    {
        _model = new(
            "SomethingId",
            "Something",
            "Something",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, _type),
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);

        _sink = Substitute.For<ISink>();
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(_type);
        _factory.CreateFor(string.Empty, string.Empty, _model).Returns(_sink);
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    async Task Because() => _result = await _sinks.GetFor(_model);

    [Fact] void should_return_the_sink() => _result.ShouldEqual(_sink);
    [Fact] void should_ensure_indexes() => _sink.Received(1).EnsureIndexes();
}
