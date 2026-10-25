// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Transactions;
using Cratis.Monads;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projections"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the projections belongs to.</param>
/// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="artifactsActivator"><see cref="IClientArtifactsActivator"/> for activating instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <param name="logger"><see cref="ILogger{Projections}"/> for logging.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IClientArtifactsProvider clientArtifacts,
    INamingPolicy namingPolicy,
    IClientArtifactsActivator artifactsActivator,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<Projections> logger) : IProjections, IKnowPassiveProjections, IKnowReadModelContainerNames
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
#if NET8_0
    readonly object _explicitLock = new();
#else
    readonly Lock _explicitLock = new();
#endif
    readonly Dictionary<Type, IExplicitProjection> _explicitProjections = [];
    readonly HashSet<Type> _registeredAtRuntime = [];
    readonly HashSet<Type> _warnedAboutRootRemovalViaJoin = [];
    Dictionary<Type, IProjectionHandler> _handlersByType = new();
    Dictionary<Type, IProjectionHandler> _handlersByModelType = new();
    Dictionary<Type, IProjectionHandler> _modelBoundHandlers = new();
    Dictionary<Type, ProjectionDefinition> _definitionsByType = new();
    Dictionary<Type, IProjectionHandler> _discoveredHandlersByModelType = new();
    Dictionary<Type, ExplicitProjectionRegistration> _explicitRegistrations = new();
    Dictionary<Type, string> _discoveredContainerNames = new();
    Dictionary<Type, string> _containerNames = new();
    Dictionary<Type, Exception> _explicitFailures = new();
    IImmutableList<ProjectionDefinition> _discoveredDefinitions = ImmutableList<ProjectionDefinition>.Empty;
    IImmutableList<ArtifactRegistration> _discoveredArtifactRegistrations = ImmutableList<ArtifactRegistration>.Empty;
    IEnumerable<IProjectionHandler> _explicitHandlers = [];
    bool _discovered;

    /// <summary>Raised after definitions are registered successfully.</summary>
    internal event Action? Registered;

    /// <summary>
    /// Gets the fallback monitor used by native registries on custom stores.
    /// </summary>
    internal object ReadModelOwnershipSync { get; } = new();

    /// <summary>
    /// Gets the generation delivery policy supplied by the event store.
    /// </summary>
    internal Observation.EventGenerationDelivery GenerationDelivery { get; init; }

    /// <summary>
    /// Gets or sets where model-bound projection definitions come from. The default discovers them; a source must hand back
    /// definitions this instance owns, because generation delivery is applied to them afterwards.
    /// </summary>
    internal IModelBoundProjectionsSource ModelBoundSource { get; set; } = ModelBoundProjectionsSource.Default;

    /// <summary>
    /// Gets the software version of the application, sent with every registration so the kernel can ignore one that
    /// is older than what it has stored. Left unset, registrations carry no version and are never ignored.
    /// </summary>
    internal string? SoftwareVersion { get; init; }

    /// <summary>
    /// Gets the identifier of the current connection, sent with every registration so the kernel knows which live
    /// connection declares the projections. It is optional: a registration without it still succeeds.
    /// </summary>
    internal string? CurrentConnectionId => eventStore.Connection?.Lifecycle?.ConnectionId?.ToString();

    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    internal IImmutableList<ProjectionDefinition> Definitions { get; private set; } = ImmutableList<ProjectionDefinition>.Empty;

    /// <summary>
    /// Gets the per-artifact outcome of the last <see cref="Discover"/>, covering every declared projection artifact -
    /// the fluent <see cref="IProjectionFor{TReadModel}"/> implementations and the model-bound read models.
    /// </summary>
    /// <remarks>
    /// An artifact with no failure has a definition and travels in the batch <see cref="Register"/> sends. This is a
    /// discovery fact rather than a verdict: it is <see cref="IEventStore.RegisterAll"/> that publishes it as an
    /// outcome, and only once the kernel call carrying these definitions has returned.
    /// </remarks>
    internal IImmutableList<ArtifactRegistration> ArtifactRegistrations { get; private set; } = ImmutableList<ArtifactRegistration>.Empty;

    /// <inheritdoc/>
    public bool HasFor(ProjectionId projectionId) => Definitions.Any(_ => _.Identifier == projectionId);

    /// <inheritdoc/>
    public bool HasFor<TReadModel>() => _handlersByModelType.ContainsKey(typeof(TReadModel));

    /// <inheritdoc/>
    public bool HasFor(Type readModelType) => _handlersByModelType.ContainsKey(readModelType);

    /// <inheritdoc/>
    public IEnumerable<IProjectionHandler> GetAllHandlers() => _handlersByType.Values.Concat(_modelBoundHandlers.Values).Concat(_explicitHandlers);

    /// <inheritdoc/>
    public IProjectionHandler GetHandlerFor<TProjection>()
        where TProjection : IProjection => _handlersByType[typeof(TProjection)];

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdFor<TProjection>()
        where TProjection : IProjection => _handlersByType[typeof(TProjection)].Id;

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel<TReadModel>() => GetProjectionIdForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel(Type readModelType) => _handlersByModelType[readModelType].Id;

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor<TProjection>()
        where TProjection : IProjection =>
            GetFailedPartitionsFor(typeof(TProjection));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor(Type projectionType)
    {
        var handler = GetHandlerForProjectionOrReadModelType(projectionType);
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateFor<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByType[projectionType];
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateForModel<TReadModel>() => GetStateForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateForModel(Type readModelType) => _handlersByModelType[readModelType].GetState();

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsForModel<TReadModel>() => GetFailedPartitionsForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsForModel(Type readModelType) => _handlersByModelType[readModelType].GetFailedPartitions();

    /// <inheritdoc/>
    public Task<JobId> Replay<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByType[projectionType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public async Task<JobId> Replay(ProjectionId projectionId)
    {
        var response = await _servicesAccessor.Services.Observers.Replay(new Contracts.Observation.Replay
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ObserverId = projectionId,
            EventSequenceId = string.Empty
        });
        return Guid.TryParse(response.JobId, out var value) ? new JobId(value) : JobId.NotSet;
    }

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward<TProjection>()
        where TProjection : IProjection => FastForward(_handlersByType[typeof(TProjection)].Id);

    /// <inheritdoc/>
    public Task<FastForwardResult> FastForward(ProjectionId projectionId) =>
        ObserverFastForwarder.FastForward(_servicesAccessor.Services.Observers, eventStore.Name, eventStore.Namespace, projectionId);

    /// <inheritdoc/>
    public IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> GetExternalEventStoreSubscriptions()
    {
        return Definitions
            .Where(d => d.EventSequenceId.StartsWith(EventSequenceId.InboxPrefix, StringComparison.Ordinal))
            .GroupBy(d => d.EventSequenceId[EventSequenceId.InboxPrefix.Length..])
            .Select(g => (
                EventStoreName: g.Key,
                EventTypeIds: g.SelectMany(d => d.From.Keys
                    .Concat(d.Join.Keys)
                    .Select(et => new EventTypeId(et.Id)))
                    .Distinct()
                    .AsEnumerable()))
            .ToList();
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        lock (ReadModelBuilderOwnership.SyncFor(eventStore, ReadModelOwnershipSync))
        {
            var catalog = new Reducers.ReducerCatalog(clientArtifacts, eventTypes, new Reducers.ReducerValidator());
            foreach (var model in clientArtifacts.Projections.Select(ProjectionTypeExtensions.GetReadModelType).Concat(clientArtifacts.ModelBoundProjections))
            {
                ReadModelBuilderOwnership.Validate(eventStore, clientArtifacts, catalog, model, model, false);
            }
            foreach (var model in _explicitProjections.Keys)
            {
                ReadModelBuilderOwnership.Validate(eventStore, clientArtifacts, catalog, model, model, false);
            }

            return DiscoverCore();
        }
    }

    /// <inheritdoc/>
    public Task<IProjectionHandler> Register<TReadModel>(Action<IProjectionBuilderFor<TReadModel>> define, ProjectionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(define);
        return RegisterExplicit<TReadModel>(new DeclarativeExplicitProjection<TReadModel>(define, id ?? ArtifactIdentity.ModelProjectionIdFor(typeof(TReadModel))));
    }

    /// <inheritdoc/>
    public Task<IProjectionHandler> Register<TReadModel>() =>
        RegisterExplicit<TReadModel>(new ModelBoundExplicitProjection(typeof(TReadModel)));

    /// <inheritdoc/>
    public async Task Register()
    {
        await _servicesAccessor.Services.Projections.Register(new()
        {
            EventStore = eventStore.Name,
            Owner = ProjectionOwner.Client,
            Projections = [.. Definitions],

            // The registration only claims to be the client's full set when discovery has run and every discovered
            // artifact produced a definition. An artifact whose definition could not be built is excluded from the
            // batch, and claiming a full set then would make the kernel retire a projection that still exists in
            // the client.
            Registrant = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty,
            Version = SoftwareVersion,
            ConnectionId = CurrentConnectionId,
            FullSet = _discovered && ArtifactRegistrations.All(registration => registration.IsRegistered)
        });
        Registered?.Invoke();
    }

    /// <inheritdoc/>
    public async Task<ProjectionQueryResult> Query(string declaration, string eventSequenceId = "event-log")
    {
        ReadProtectionGuard.Read(nameof(Query));
        var result = await _servicesAccessor.Services.Projections.Preview(new PreviewProjectionRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = eventSequenceId,
            Declaration = declaration
        });

        if (result.Value1 is not null)
        {
            throw new UnableToQueryProjection(result.Value1.Errors.Select(e => e.Message));
        }

        var queryResult = result.Value0!;
        return new ProjectionQueryResult([.. queryResult.ReadModelEntries]);
    }

    /// <inheritdoc/>
    bool IKnowPassiveProjections.IsPassive(Type readModelType) =>
        _handlersByModelType.TryGetValue(readModelType, out var handler) &&
        Definitions.Any(definition => definition.Identifier == handler.Id.Value && !definition.IsActive);

    /// <inheritdoc/>
    string? IKnowReadModelContainerNames.GetContainerNameOverride(Type readModelType) =>
        _containerNames.GetValueOrDefault(readModelType);

    /// <summary>
    /// Add an explicit projection that is built by the next <see cref="Discover"/> rather than immediately.
    /// </summary>
    /// <param name="projection">The <see cref="IExplicitProjection"/> to add.</param>
    /// <remarks>
    /// This is how a registration made while the client is being configured reaches the event store: at that point the
    /// event types the definition refers to are not known yet, so building it is left to discovery, which knows them.
    /// </remarks>
    internal void Add(IExplicitProjection projection)
    {
        lock (_explicitLock)
        {
            _explicitProjections[projection.ReadModelType] = projection;
        }
    }

    /// <summary>
    /// Forget the projections registered explicitly on this event store after it was created, keeping the ones it
    /// received from the client's options.
    /// </summary>
    /// <remarks>
    /// This takes them out of what this client declares, so the next registration of the full set retires them in
    /// Chronicle like any other projection the client stops declaring. It exists for hosts that reuse one event store
    /// across isolated runs, such as a specification fixture resetting between specifications.
    /// </remarks>
    internal void ForgetRuntimeRegistrations()
    {
        lock (_explicitLock)
        {
            foreach (var readModelType in _registeredAtRuntime)
            {
                _explicitProjections.Remove(readModelType);
            }

            _registeredAtRuntime.Clear();
            BuildAllExplicitProjections();
            Compose();
        }
    }

    /// <summary>
    /// Resolve the <see cref="IProjectionHandler"/> for a type that is either a projection type or a read model type.
    /// </summary>
    /// <param name="type">The projection type or read model type to resolve for.</param>
    /// <returns>The <see cref="IProjectionHandler"/> for the type.</returns>
    /// <remarks>
    /// Fluent projections are addressable by their own type, while model-bound projections have no projection type at
    /// all - their handler is only ever keyed by the read model it projects to. A caller holding either handle has to
    /// land on the same handler, so the projection type is tried first and the read model type second.
    /// </remarks>
    IProjectionHandler GetHandlerForProjectionOrReadModelType(Type type) =>
        _handlersByType.TryGetValue(type, out var handler) ? handler : _handlersByModelType[type];

    Task DiscoverCore()
    {
        var (modelBoundDefinitions, modelBoundFailures) = ModelBoundSource.Discover(clientArtifacts, namingPolicy, eventTypes, logger, eventStore.Name?.Value);
        ApplyGenerationDelivery(modelBoundDefinitions, modelBoundFailures);
        var (definitionsByType, failures, containerNames) = FindAllProjectionDefinitions(
            eventTypes,
            clientArtifacts,
            artifactsActivator,
            jsonSerializerOptions);
        _definitionsByType = definitionsByType;

        foreach (var (projectionType, definition) in definitionsByType)
        {
            WarnAboutRootRemovalViaJoin(projectionType.GetReadModelType(), definition);
        }

        foreach (var (readModelType, definition) in modelBoundDefinitions)
        {
            WarnAboutRootRemovalViaJoin(readModelType, definition);
        }

        _handlersByType = _definitionsByType.ToDictionary(
                kvp => kvp.Key,
                kvp => CreateHandler(kvp.Value, kvp.Key.GetReadModelType(), containerNames.GetValueOrDefault(kvp.Key.GetReadModelType())) as IProjectionHandler);

        _modelBoundHandlers = modelBoundDefinitions.ToDictionary(
            kvp => kvp.Key,
            kvp => CreateHandler(kvp.Value, kvp.Key, containerNames.GetValueOrDefault(kvp.Key)) as IProjectionHandler);

        // The read model index holds one handler per read model, and asking for a projection by the model it maintains
        // is the only handle a model-bound projection has. So a read model claimed twice leaves the second projection
        // addressable only if it has a type of its own - and a model-bound one does not. Both are still registered and
        // both still write to the read model, which is the part worth knowing about; say so rather than resolving it
        // silently by declaration order.
        var discoveredHandlersByModelType = new Dictionary<Type, IProjectionHandler>();
        var claimedBy = new Dictionary<Type, string>();

        foreach (var kvp in _handlersByType)
        {
            var readModelType = kvp.Key.GetReadModelType();
            if (discoveredHandlersByModelType.TryAdd(readModelType, kvp.Value))
            {
                claimedBy[readModelType] = kvp.Key.FullName ?? kvp.Key.Name;
            }
            else
            {
                logger.MoreThanOneProjectionForReadModel(readModelType, kvp.Key.FullName ?? kvp.Key.Name, claimedBy[readModelType]);
            }
        }

        foreach (var kvp in _modelBoundHandlers)
        {
            if (!discoveredHandlersByModelType.TryAdd(kvp.Key, kvp.Value))
            {
                logger.MoreThanOneProjectionForReadModel(kvp.Key, $"the model-bound projection on {kvp.Key.Name}", claimedBy[kvp.Key]);
            }
        }

        lock (_explicitLock)
        {
            _discoveredHandlersByModelType = discoveredHandlersByModelType;
            _discoveredContainerNames = containerNames;

            _discoveredDefinitions =
                ((IEnumerable<ProjectionDefinition>)[
                    .. _definitionsByType.Values.Select(_ => _).ToList(),
                    .. modelBoundDefinitions.Values
                ]).ToImmutableList();

            _discoveredArtifactRegistrations =
                ((IEnumerable<ArtifactRegistration>)[
                    .. _definitionsByType.Keys.Select(type => new ArtifactRegistration(type, null)),
                    .. modelBoundDefinitions.Keys.Select(type => new ArtifactRegistration(type, null)),
                    .. failures.Select(kvp => new ArtifactRegistration(kvp.Key, kvp.Value)),
                    .. modelBoundFailures.Select(kvp => new ArtifactRegistration(kvp.Key, kvp.Value))
                ]).ToImmutableList();

            BuildAllExplicitProjections();
            Compose();
        }

        _discovered = true;
        return Task.CompletedTask;
    }

    async Task<IProjectionHandler> RegisterExplicit<TReadModel>(IExplicitProjection projection)
    {
        ExplicitProjectionRegistration registration;
        lock (ReadModelBuilderOwnership.SyncFor(eventStore, ReadModelOwnershipSync))
        {
            lock (_explicitLock)
            {
                var readModelType = projection.ReadModelType;
                ReadModelBuilderOwnership.Validate(eventStore, clientArtifacts, new Reducers.ReducerCatalog(clientArtifacts, eventTypes, new Reducers.ReducerValidator()), readModelType, readModelType, false);
                if (_discoveredHandlersByModelType.TryGetValue(readModelType, out var discovered))
                {
                    // Asking for a discovered model-bound read model to be registered is asking for what is already there.
                    if (projection.IsModelBound && discovered.Id == projection.Id)
                    {
                        return discovered;
                    }

                    throw new ReadModelAlreadyHasProjection(readModelType, discovered.Id, projection.Id);
                }

                if (_explicitRegistrations.TryGetValue(readModelType, out var existing) && existing.Handler.Id != projection.Id)
                {
                    throw new ReadModelAlreadyHasProjection(readModelType, existing.Handler.Id, projection.Id);
                }

                // Built here rather than deferred so that a mistake in the definition reaches the caller who made it.
                registration = Build(projection);
                _explicitProjections[readModelType] = projection;
                _registeredAtRuntime.Add(readModelType);
                _explicitRegistrations = new(_explicitRegistrations) { [readModelType] = registration };
                _explicitFailures = _explicitFailures.Where(_ => _.Key != readModelType).ToDictionary(_ => _.Key, _ => _.Value);
                Compose();
            }
        }

        // Before the connection is up there is nothing to send to - the registration pass that runs once it is up
        // includes this projection, because it is now part of the definitions that pass sends.
        if (eventStore.Connection.Lifecycle.IsConnected)
        {
            // Read models are registered before the projections that maintain them, as the full registration pass does.
            await eventStore.ReadModels.Register<TReadModel>();
            await _servicesAccessor.Services.Projections.Register(new()
            {
                EventStore = eventStore.Name,
                Owner = ProjectionOwner.Client,
                Projections = [registration.Definition],
                Registrant = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty,
                Version = SoftwareVersion,
                ConnectionId = CurrentConnectionId,
                FullSet = false
            });
            Registered?.Invoke();
        }

        return registration.Handler;
    }

    /// <summary>
    /// Build every explicit projection anew, recording the ones that cannot be built rather than failing discovery.
    /// </summary>
    /// <remarks>
    /// Must be called while holding the explicit lock. An explicit projection maintaining a read model that discovery
    /// found is dropped when it is the very same model-bound projection, and reported as a failure otherwise.
    /// </remarks>
    void BuildAllExplicitProjections()
    {
        var registrations = new Dictionary<Type, ExplicitProjectionRegistration>();
        var failures = new Dictionary<Type, Exception>();
        foreach (var (readModelType, projection) in _explicitProjections)
        {
            if (_discoveredHandlersByModelType.TryGetValue(readModelType, out var discovered))
            {
                if (!(projection.IsModelBound && discovered.Id == projection.Id))
                {
                    failures[readModelType] = new ReadModelAlreadyHasProjection(readModelType, discovered.Id, projection.Id);
                    logger.FailedToCreateExplicitProjectionDefinition(readModelType, failures[readModelType]);
                }

                continue;
            }

            try
            {
                registrations[readModelType] = Build(projection);
            }
#pragma warning disable CA1031 // One unbuildable read model must not be able to take the rest of the read side with it.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.FailedToCreateExplicitProjectionDefinition(readModelType, ex);
                failures[readModelType] = ex;
            }
        }

        _explicitRegistrations = registrations;
        _explicitFailures = failures;
    }

    void WarnAboutRootRemovalViaJoin(Type readModelType, ProjectionDefinition definition)
    {
        var removedWithJoin = RootRemovalViaJoin.EventTypesFor(definition);
        if (removedWithJoin.Count == 0)
        {
            return;
        }

        lock (_warnedAboutRootRemovalViaJoin)
        {
            if (!_warnedAboutRootRemovalViaJoin.Add(readModelType))
            {
                return;
            }
        }

        logger.RootRemovalViaJoinNotSupported(definition.Identifier, readModelType, string.Join(", ", removedWithJoin));
    }

    ExplicitProjectionRegistration Build(IExplicitProjection projection)
    {
        var definition = projection.Build(new(namingPolicy, eventTypes, jsonSerializerOptions, eventStore.Name?.Value, GenerationDelivery));
        ProjectionGenerationDeliveryValidation.Apply(definition, GenerationDelivery);
        WarnAboutRootRemovalViaJoin(projection.ReadModelType, definition);
        var handler = CreateHandler(definition, projection.ReadModelType, projection.ContainerNameOverride);
        return new(definition, handler, projection.ContainerNameOverride);
    }

    ProjectionHandler CreateHandler(ProjectionDefinition definition, Type readModelType, string? containerName) =>
        new(eventStore, definition.Identifier, readModelType, containerName ?? namingPolicy.GetReadModelName(readModelType), definition.EventSequenceId);

    /// <summary>
    /// Compose what discovery found with what was registered explicitly into the state everything else reads.
    /// </summary>
    /// <remarks>
    /// Must be called while holding the explicit lock. Every published collection is replaced rather than mutated, so a
    /// reader never sees one half-way through being composed.
    /// </remarks>
    void Compose()
    {
        var handlersByModelType = new Dictionary<Type, IProjectionHandler>(_discoveredHandlersByModelType);
        foreach (var (readModelType, registration) in _explicitRegistrations)
        {
            handlersByModelType[readModelType] = registration.Handler;
        }

        _explicitHandlers = [.. _explicitRegistrations.Values.Select(_ => _.Handler)];
        _handlersByModelType = handlersByModelType;

        var containerNames = new Dictionary<Type, string>(_discoveredContainerNames);
        foreach (var (readModelType, registration) in _explicitRegistrations)
        {
            if (registration.ContainerName is not null)
            {
                containerNames[readModelType] = registration.ContainerName;
            }
        }

        _containerNames = containerNames;
        Definitions = _discoveredDefinitions.AddRange(_explicitRegistrations.Values.Select(_ => _.Definition));
        ArtifactRegistrations = _discoveredArtifactRegistrations
            .AddRange(_explicitRegistrations.Keys.Select(type => new ArtifactRegistration(type, null)))
            .AddRange(_explicitFailures.Select(kvp => new ArtifactRegistration(kvp.Key, kvp.Value)));
    }

    /// <summary>
    /// Builds a definition for every declared fluent projection, isolating the ones that cannot be built.
    /// </summary>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> holding the declared projections.</param>
    /// <param name="artifactsActivator"><see cref="IClientArtifactsActivator"/> for activating instances of projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <returns>The definitions that could be built, and the failure that stopped each one that could not.</returns>
    /// <remarks>
    /// A projection that cannot be built is logged and skipped so that it costs itself and nothing else. The failure is
    /// returned alongside rather than only logged, so that its read model can be told apart from one never declared.
    /// </remarks>
    (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames) FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IClientArtifactsActivator artifactsActivator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var result = new Dictionary<Type, ProjectionDefinition>();
        var failures = new Dictionary<Type, Exception>();
        var variantDeclarations = new Dictionary<Type, FluentVariantDeclaration>();
        var declaredContainerNames = new Dictionary<Type, string>();
        foreach (var projectionType in clientArtifacts.Projections)
        {
            var modelType = projectionType.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
            var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
            var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
            var createProjectionDefinitionResult = (method.Invoke(
                null,
                [
                    projectionType,
                    namingPolicy,
                    eventTypes,
                    artifactsActivator,
                    jsonSerializerOptions,
                    variantDeclarations,
                    declaredContainerNames,
                    GenerationDelivery
                ]) as Catch<ProjectionDefinition>)!;
            if (createProjectionDefinitionResult.TryGetException(out var exception))
            {
                logger.FailedToCreateProjectionDefinition(projectionType, exception);
                failures[projectionType] = exception;
                continue;
            }
            result.Add(projectionType, createProjectionDefinitionResult.AsT0);
        }

        VariantReclassifier.CrossWireGroups(result, variantDeclarations);
        ApplyGenerationDelivery(result, failures);

        // An override only means something while the projection that declared it is still defined. Revalidation may have
        // removed that projection, and a stale override must neither leak into the composed container map nor be skipped
        // over when the handler for the read model reports its container.
        var containerNames = result.Keys
            .Where(declaredContainerNames.ContainsKey)
            .GroupBy(type => type.GetReadModelType(), type => declaredContainerNames[type])
            .ToDictionary(group => group.Key, group => group.First());

        return (result, failures, containerNames);
    }

    void ApplyGenerationDelivery(IDictionary<Type, ProjectionDefinition> definitions, IDictionary<Type, Exception> failures)
    {
        foreach (var (type, definition) in definitions.ToArray())
        {
            try
            {
                ProjectionGenerationDeliveryValidation.Apply(definition, GenerationDelivery);
            }
#pragma warning disable CA1031 // One invalid projection must not discard the other definitions.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.FailedToCreateProjectionDefinition(type, exception);
                failures[type] = exception;
                definitions.Remove(type);
            }
        }
    }

    sealed record ExplicitProjectionRegistration(ProjectionDefinition Definition, IProjectionHandler Handler, string? ContainerName);

    static class ProjectionDefinitionCreator<TReadModel>
        where TReadModel : class
    {
        public static Catch<ProjectionDefinition> CreateAndDefine(
            Type type,
            INamingPolicy namingPolicy,
            IEventTypes eventTypes,
            IClientArtifactsActivator artifactsActivator,
            JsonSerializerOptions jsonSerializerOptions,
            IDictionary<Type, FluentVariantDeclaration> variantDeclarations,
            IDictionary<Type, string> declaredContainerNames,
            Observation.EventGenerationDelivery generationDelivery)
        {
            try
            {
                var activateArtifactResult = artifactsActivator.ActivateNonDisposable<IProjectionFor<TReadModel>>(type);
                if (activateArtifactResult.TryGetException(out var exception))
                {
                    return exception;
                }

                var builder = new ProjectionBuilderFor<TReadModel>(type.GetProjectionId(), type, namingPolicy, eventTypes, jsonSerializerOptions) { GenerationDelivery = generationDelivery };
                activateArtifactResult.AsT0.Define(builder);
                var definition = builder.Build();

                if (builder.ContainerNameOverride is { } containerName)
                {
                    var existing = declaredContainerNames.Where(declared => declared.Key.GetReadModelType() == typeof(TReadModel)).Select(declared => declared.Value).FirstOrDefault();
                    if (existing is not null && existing != containerName)
                    {
                        throw new ConflictingContainerNamesForReadModel(typeof(TReadModel), existing, containerName);
                    }

                    declaredContainerNames[type] = containerName;
                }

                if (builder.VariantDeclaration is not null)
                {
                    variantDeclarations[type] = builder.VariantDeclaration;
                }

                return definition;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
