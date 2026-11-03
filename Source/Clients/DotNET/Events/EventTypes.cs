// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public partial class EventTypes : IEventTypes
{
    readonly IEventStore _eventStore;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IEventTypeMigrators _eventTypeMigrators;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly bool _enableEventTypeGenerationValidation;
    readonly INamingPolicy _namingPolicy;
    readonly AsyncLocal<bool> _discovering = new();
    volatile Task _schemaVerdict = Task.CompletedTask;
    IServiceProvider? _migrationServices;
    IEventTypeMigrators? _storeMigrators;
    ExceptionDispatchInfo? _payloadFailure;
    FrozenDictionary<Type, EventTypeDescriptor> _descriptors = FrozenDictionary<Type, EventTypeDescriptor>.Empty;
    FrozenDictionary<EventType, Type> _typesByEventType = FrozenDictionary<EventType, Type>.Empty;
    FrozenDictionary<EventType, JsonSchema> _schemasByEventType = FrozenDictionary<EventType, JsonSchema>.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="EventTypes"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the event types belong to.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas from types.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="eventTypeMigrators"><see cref="IEventTypeMigrators"/> for discovering event type migrators.</param>
    /// <param name="enableEventTypeGenerationValidation">Whether to enable event type generation chain validation on the server. Defaults to <see langword="false"/>.</param>
    public EventTypes(
        IEventStore eventStore,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IClientArtifactsProvider clientArtifacts,
        IEventTypeMigrators eventTypeMigrators,
        bool enableEventTypeGenerationValidation = false)
        : this(eventStore, jsonSchemaGenerator, clientArtifacts, eventTypeMigrators, enableEventTypeGenerationValidation, new DefaultNamingPolicy())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EventTypes"/> with the naming policy used for event payloads.
    /// </summary>
    /// <param name="eventStore">The event store the event types belong to.</param>
    /// <param name="jsonSchemaGenerator">The generator for event schemas.</param>
    /// <param name="clientArtifacts">The provider of client artifacts.</param>
    /// <param name="eventTypeMigrators">The migrator discovery service.</param>
    /// <param name="enableEventTypeGenerationValidation">Whether to validate the generation chain on the server.</param>
    /// <param name="namingPolicy">The naming policy, or null to preserve declared property names.</param>
    public EventTypes(
        IEventStore eventStore,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IClientArtifactsProvider clientArtifacts,
        IEventTypeMigrators eventTypeMigrators,
        bool enableEventTypeGenerationValidation,
        INamingPolicy? namingPolicy)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _clientArtifacts = clientArtifacts;
        _eventTypeMigrators = eventTypeMigrators;
        _enableEventTypeGenerationValidation = enableEventTypeGenerationValidation;
        _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();
    }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes => _typesByEventType.Values.ToImmutableList();

    /// <inheritdoc/>
    public IImmutableList<EventType> All => _typesByEventType.Keys.ToImmutableList();

    /// <summary>
    /// Gets the immutable descriptor snapshot used to scope routing caches.
    /// </summary>
    internal FrozenDictionary<Type, EventTypeDescriptor> Descriptors => _descriptors;

    /// <summary>
    /// Gets the immutable registered identities in their discovery order.
    /// </summary>
    internal FrozenDictionary<EventType, Type> RegisteredTypes => _typesByEventType;

    /// <inheritdoc/>
    /// <exception cref="MultipleEventTypesWithSameIdFound">
    /// Thrown when more than one CLR type resolves to the same event type id and generation.
    /// </exception>
    /// <exception cref="ArtifactPreparationReentered">Discovery reenters its own unfinished schema generation.</exception>
    public Task Discover() => RunLocalPreparation(DiscoverLocal);

    /// <inheritdoc/>
    /// <exception cref="ArtifactPreparationReentered">Registration reenters unfinished local preparation.</exception>
    public async Task Register()
    {
        if (_discovering.Value) throw new ArtifactPreparationReentered();
        if (_eventStore is EventStore store)
        {
            store.ThrowIfArtifactPreparationReentered();
            await store.LatestPreparationVerdict;
        }
        var payload = await PrepareRegistration();
        await RegisterPrepared(payload);
    }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    /// <exception cref="TypeIsNotAnEventType">Thrown when the type is not a registered event type.</exception>
    /// <remarks>
    /// Reporting the miss as <see cref="TypeIsNotAnEventType"/> rather than letting the lookup fall out as a LINQ
    /// exception matters most to the model-bound projection attributes: their generic type argument is
    /// unconstrained, so nothing at compile time relates it to an event type and this is the first place the
    /// mistake can be named. The fluent projection builder already answers the same condition the same way.
    /// </remarks>
    public EventType GetEventTypeFor(Type clrType)
    {
        foreach (var (eventType, type) in _typesByEventType)
        {
            if (type == clrType)
            {
                return eventType;
            }
        }

        throw new TypeIsNotAnEventType(clrType);
    }

    /// <inheritdoc/>
    public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => _schemasByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => _typesByEventType
        .Where(_ => _.Key.Id == eventTypeId && _.Key.Generation == generation)
        .Select(_ => _.Value)
        .FirstOrDefault() ?? GetClrTypeFor(eventTypeId);

    /// <summary>
    /// Gets this registry's descriptor for a CLR type.
    /// </summary>
    /// <param name="clrType">The registered CLR type.</param>
    /// <returns>The descriptor.</returns>
    /// <exception cref="TypeIsNotAnEventType">The CLR type is not registered.</exception>
    internal EventTypeDescriptor GetDescriptorFor(Type clrType) =>
        _descriptors.TryGetValue(clrType, out var descriptor) ? descriptor : throw new TypeIsNotAnEventType(clrType);

    /// <summary>
    /// Runs local preparation under the same reentrancy guard and schema verdict as <see cref="Discover"/>.
    /// </summary>
    /// <param name="prepare">The local preparation to run.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArtifactPreparationReentered">Preparation reenters its own unfinished local preparation.</exception>
    Task RunLocalPreparation(Action prepare)
    {
        if (_discovering.Value) throw new ArtifactPreparationReentered();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _schemaVerdict = completion.Task;
        _payloadFailure = null;
        _discovering.Value = true;
        try
        {
            prepare();
            completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
            throw;
        }
        finally
        {
            _discovering.Value = false;
        }

        return Task.CompletedTask;
    }
}
