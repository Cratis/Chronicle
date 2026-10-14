// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Discovery;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

public class an_explicit_store : Specification
{
    protected EventStore _store;
    protected ChronicleOptions _options;
    protected IClientArtifactsProvider _artifacts;
    protected IChronicleConnection _connection;
    protected IServices _services;
    protected ServiceProvider _provider;
    protected SchemaGenerator _generator;
    protected IClientArtifactsActivator _activator;
    protected IReactorSideEffectHandlers _registeredSideEffectHandlers;
    protected RegisterEventTypesRequest _payload;
    protected bool _auto;
    protected ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    void Establish()
    {
        _options = new() { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
        _activator = Substitute.For<IClientArtifactsActivator>();
        _registeredSideEffectHandlers = Substitute.For<IReactorSideEffectHandlers>();
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        _connection.Lifecycle.Returns(new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance));
        _services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)_connection).Services.Returns(_services);
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.NewGuid()));
        _services.Seeding.SeedEvents(Arg.Any<SeedEventsRequest>()).Returns(CommandResult.Success(Guid.NewGuid()));
        _services.EventTypes.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()).Returns(call =>
        {
            _payload = call.Arg<RegisterEventTypesRequest>();
            return CommandResult.Success(Guid.NewGuid());
        });
        var registrations = new ServiceCollection();
        registrations.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        registrations.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        registrations.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = registrations.BuildServiceProvider();
        var compliance = new ComplianceMetadataResolver(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>());
        _generator = new(new JsonSchemaGenerator(compliance, new DefaultNamingPolicy()));
    }

    protected EventStore CreateStore() => new(
        "current",
        "default",
        _connection,
        _artifacts,
        new EventTypeMigrators(_artifacts, _provider),
        Substitute.For<ICorrelationIdAccessor>(),
        Substitute.For<IConcurrencyScopeStrategies>(),
        Substitute.For<ICausationManager>(),
        Substitute.For<IIdentityProvider>(),
        _generator,
        new DefaultNamingPolicy(),
        _provider,
        _registeredSideEffectHandlers,
        _activator,
        _auto,
        JsonSerializerOptions.Default,
        false,
        Options.Create(_options),
        _loggerFactory);

    void Destroy() => _provider.Dispose();

    public record PlainEvent(string Name);

    public record AnotherEvent(string Name);

    [CompensationFor<PlainEvent>]
    public record Compensation(string Name);

    public class SchemaGenerator(JsonSchemaGenerator inner) : IJsonSchemaGenerator
    {
        public Action<Type, Events.IEventTypes, bool>? BeforeGenerate { get; set; }

        public JsonSchema Generate(Type type) => inner.Generate(type);

        public JsonSchema GenerateForReadModel(Type type) => inner.GenerateForReadModel(type);

        public JsonSchema GenerateLegacyEventType(Type type) => inner.GenerateLegacyEventType(type);

        public JsonSchema Generate(Type type, Events.IEventTypes eventTypes)
        {
            BeforeGenerate?.Invoke(type, eventTypes, false);
            return inner.Generate(type, eventTypes);
        }

        public JsonSchema GenerateLegacyEventType(Type type, Events.IEventTypes eventTypes)
        {
            BeforeGenerate?.Invoke(type, eventTypes, true);
            return inner.GenerateLegacyEventType(type, eventTypes);
        }
    }
}
