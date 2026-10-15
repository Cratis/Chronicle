// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_creating;

public class and_event_metadata_uses_a_known_read_model_sink : Specification
{
    Sinks _sinks;
    ISinkFactory _factory;
    ReadModelDefinition _target;
    Exception _error;

    void Establish()
    {
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(WellKnownSinkTypes.MongoDB);
        _sinks = new("store", "tenant", new KnownInstancesOf<ISinkFactory>([_factory]));
        _target = new(
            "target", "target", "Target", ReadModelOwner.None, ReadModelSource.Unknown, ReadModelObserverType.NotSet, ReadModelObserverIdentifier.Unspecified, new(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB, new(new EventType("PublicStateChanged", EventTypeGeneration.First))), new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), []);
        _factory.ClearReceivedCalls();
    }

    async Task Because() => _error = await Catch.Exception(() => _sinks.GetFor(_target));

    [Fact] void should_refuse_to_ignore_event_metadata() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] void should_not_invoke_the_read_model_factory() => _factory.ReceivedCalls().ShouldBeEmpty();
}
