// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.given;

public abstract class named_sinks : Specification
{
    protected static readonly SinkTypeId _defaultType = "MongoDB";
    protected static readonly SinkTypeId _namedType = "SQL";

    protected Sinks _sinks;
    protected ISinkFactory _defaultFactory;
    protected ISinkFactory _namedFactory;
    protected ISinkConfigurations _configurations;
    protected ISink _defaultSink;
    protected ISink _reportingSink;
    protected ISink _archiveSink;

    void Establish()
    {
        _defaultSink = Substitute.For<ISink>();
        _reportingSink = Substitute.For<ISink>();
        _archiveSink = Substitute.For<ISink>();
        _defaultFactory = Substitute.For<ISinkFactory>();
        _defaultFactory.TypeId.Returns(_defaultType);
        _defaultFactory.CreateFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>()).Returns(_defaultSink);
        _namedFactory = Substitute.For<ISinkFactory>();
        _namedFactory.TypeId.Returns(_namedType);
        _namedFactory.CreateFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>(), new SinkName("reporting")).Returns(_reportingSink);
        _namedFactory.CreateFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>(), new SinkName("archive")).Returns(_archiveSink);
        _configurations = Substitute.For<ISinkConfigurations>();
        _configurations.ResolveFor(Arg.Any<ReadModelDefinition>(), Arg.Any<bool>()).Returns(call =>
        {
            var name = call.Arg<ReadModelDefinition>().Sink.Name;
            return name.IsDefault ? new ResolvedSink(_defaultType, SinkName.Default) : new ResolvedSink(_namedType, name);
        });
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_defaultFactory, _namedFactory]), _configurations);
    }

    protected static ReadModelDefinition ReadModel(SinkName name) => new(
        "orders",
        "orders",
        "Orders",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new SinkDefinition(SinkConfigurationId.None, _defaultType) { Name = name },
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        []);
}
