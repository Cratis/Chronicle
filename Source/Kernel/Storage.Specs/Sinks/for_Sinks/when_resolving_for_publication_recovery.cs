// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_resolving_for_publication_recovery : Specification
{
    Sinks _sinks;
    ISink _sink;
    ReadModelDefinition _model;
    int _initializationsBeforeNormalUse;
    void Establish()
    {
        _model = new("model", "Model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Reducer, ReadModelObserverIdentifier.Unspecified, new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB), new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        _sink = Substitute.For<ISink>();
        var factory = Substitute.For<ISinkFactory>();
        factory.TypeId.Returns(WellKnownSinkTypes.MongoDB);
        factory.CreateFor("store", "namespace", _model).Returns(_sink);
        _sinks = new("store", "namespace", new KnownInstancesOf<ISinkFactory>([factory]));
    }
    async Task Because()
    {
        await _sinks.GetFor(_model, ensureIndexes: false);
        _initializationsBeforeNormalUse = _sink.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(ISink.EnsureIndexes));
        await _sinks.GetFor(_model);
    }
    [Fact] void should_not_recreate_a_missing_container_during_recovery() => _initializationsBeforeNormalUse.ShouldEqual(0);
    [Fact] void should_still_initialize_the_cached_sink_before_ordinary_use() => _sink.Received(1).EnsureIndexes();
}
