// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_getting_for_explicit_container : Specification
{
    static readonly SinkTypeId _type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    ReadModelDefinition _model;
    ISinkFactory _factory;
    ISink _primarySink;
    ISink _generationSink;
    Sinks _sinks;
    ISink _primary;
    ISink _generation;
    ISink _generationAgain;

    void Establish()
    {
        _model = new("model", "Model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, new SinkDefinition(SinkConfigurationId.None, _type), new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        _primarySink = Substitute.For<ISink>();
        _generationSink = Substitute.For<ISink>();
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(_type);
        _factory.CreateFor(string.Empty, string.Empty, Arg.Is<ReadModelDefinition>(_ => _.ContainerName == "Model")).Returns(_primarySink);
        _factory.CreateFor(string.Empty, string.Empty, Arg.Is<ReadModelDefinition>(_ => _.ContainerName == "Model-g2")).Returns(_generationSink);
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    async Task Because()
    {
        _primary = await _sinks.GetFor(_model);
        _generation = await _sinks.GetFor(_model, "Model-g2");
        _generationAgain = await _sinks.GetFor(_model, "Model-g2");
    }

    [Fact] void should_create_a_distinct_sink_for_the_explicit_container() => _generation.ShouldNotBeSame(_primary);
    [Fact] void should_address_the_explicit_container() => _generation.ShouldBeSame(_generationSink);
    [Fact] void should_keep_the_read_models_own_sink() => _primary.ShouldBeSame(_primarySink);
    [Fact] void should_cache_the_sink_for_the_explicit_container() => _generationAgain.ShouldBeSame(_generation);
    [Fact] void should_not_change_the_read_model_definition_it_was_given() => _model.ContainerName.ShouldEqual(new ReadModelContainerName("Model"));
}
