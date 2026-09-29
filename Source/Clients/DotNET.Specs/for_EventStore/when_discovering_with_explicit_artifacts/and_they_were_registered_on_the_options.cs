// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_EventStore.when_discovering_with_explicit_artifacts;

public class and_they_were_registered_on_the_options : Specification
{
    EventStore _eventStore;
    ServiceProvider _provider;
    ChronicleOptions _options;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(Substitute.For<IServices>());

        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ItemAdded), typeof(CustomerRegistered)]);
        artifacts.Projections.Returns([]);
        artifacts.ModelBoundProjections.Returns([]);
        artifacts.Reactors.Returns([]);
        artifacts.Reducers.Returns([]);

        var schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());
        schemaGenerator.GenerateForReadModel(Arg.Any<Type>()).Returns(new JsonSchema());

        var servicesForClient = new ServiceCollection();
        servicesForClient.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = servicesForClient.BuildServiceProvider();

        _options = new ChronicleOptions();
        _options.ExplicitArtifacts
            .RegisterProjection<Inventory>(projection => projection.From<ItemAdded>())
            .RegisterReadModel<Customer>()
            .RegisterReactor("inventory-reactor", reactor => reactor.On<ItemAdded>(_ => { }));

        _eventStore = new EventStore(
            "store",
            "default",
            connection,
            artifacts,
            Substitute.For<IEventTypeMigrators>(),
            Substitute.For<ICorrelationIdAccessor>(),
            Substitute.For<IConcurrencyScopeStrategies>(),
            Substitute.For<ICausationManager>(),
            Substitute.For<IIdentityProvider>(),
            schemaGenerator,
            new DefaultNamingPolicy(),
            _provider,
            Substitute.For<IReactorSideEffectHandlers>(),
            Substitute.For<IClientArtifactsActivator>(),
            false,
            JsonSerializerOptions.Default,
            false,
            Options.Create(_options),
            NullLoggerFactory.Instance);
    }

    async Task Because()
    {
        await _eventStore.EventTypes.Discover();
        await _eventStore.Projections.Discover();
        await _eventStore.Reactors.Discover();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_know_the_read_model_with_a_declarative_projection() => _eventStore.Projections.HasFor<Inventory>().ShouldBeTrue();
    [Fact] void should_know_the_model_bound_read_model() => _eventStore.Projections.HasFor<Customer>().ShouldBeTrue();
    [Fact] void should_hold_both_definitions() => ((Projections.Projections)_eventStore.Projections).Definitions.Count.ShouldEqual(2);
    [Fact] void should_know_the_reactor() => _eventStore.Reactors.GetHandlerById("inventory-reactor").EventTypes.ShouldContainOnly([typeof(ItemAdded).GetEventType()]);

    [EventType]
    public record ItemAdded(string Name);

    [EventType]
    public record CustomerRegistered(string Name);

    public record Inventory(string Name);

    [FromEvent<CustomerRegistered>]
    public record Customer(string Name);
}
