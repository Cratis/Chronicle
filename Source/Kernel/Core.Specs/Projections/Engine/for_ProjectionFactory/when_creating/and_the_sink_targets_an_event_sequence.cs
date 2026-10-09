// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_sink_targets_an_event_sequence : Specification
{
    IStorage _storage;
    ProjectionFactory _factory;
    ReadModelDefinition _target;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _factory = new(
            Substitute.For<IReadModelPropertyExpressionResolvers>(),
            Substitute.For<IEventValueProviderExpressionResolvers>(),
            Substitute.For<IKeyExpressionResolvers>(),
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<IKeyResolvers>(),
            _storage,
            NullLogger<ProjectionFactory>.Instance);
        _target = new(
            "target",
            "target",
            "Target",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence),
            new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(),
            []);
    }

    async Task Because() => _error = await Catch.Exception(() => _factory.Create("store", "tenant", null!, _target, []));

    [Fact] void should_fail_explicitly_before_creating_a_read_model_projection() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] void should_not_access_any_event_store() => _storage.ReceivedCalls().ShouldBeEmpty();
}
