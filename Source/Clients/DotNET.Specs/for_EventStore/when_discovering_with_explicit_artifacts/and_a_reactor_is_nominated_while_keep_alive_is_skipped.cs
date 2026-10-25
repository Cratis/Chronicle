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
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_EventStore.when_discovering_with_explicit_artifacts;

public class and_a_reactor_is_nominated_while_keep_alive_is_skipped : Specification
{
    EventStore _eventStore;
    ServiceProvider _provider;
    ChronicleOptions _options;
    Exception _error;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IChronicleServicesAccessor)connection).Services.Returns(Substitute.For<IServices>());
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);

        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ItemAdded), typeof(CustomerRegistered)]);
        artifacts.Projections.Returns([]);
        artifacts.ModelBoundProjections.Returns([]);
        artifacts.Reactors.Returns([]);
        artifacts.Reducers.Returns([]);

        var schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());
        schemaGenerator.Generate(Arg.Any<Type>(), Arg.Any<IEventTypes>()).Returns(call => schemaGenerator.Generate(call.Arg<Type>()));
        schemaGenerator.GenerateForReadModel(Arg.Any<Type>()).Returns(new JsonSchema());

        var servicesForClient = new ServiceCollection();
        servicesForClient.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = servicesForClient.BuildServiceProvider();

        _options = new ChronicleOptions();
        _options.ExplicitArtifacts
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

    async Task Because() => _error = await Catch.Exception(_eventStore.DiscoverAll);

    void Destroy() => _provider.Dispose();

    [Fact] void should_require_keep_alive() => _error.ShouldBeOfExactType<ClientObserversRequireKeepAlive>();
    [Fact] void should_name_the_reactor() => _error.Message.Contains("inventory-reactor").ShouldBeTrue();

    [EventType]
    public record ItemAdded(string Name);

    [EventType]
    public record CustomerRegistered(string Name);
}
