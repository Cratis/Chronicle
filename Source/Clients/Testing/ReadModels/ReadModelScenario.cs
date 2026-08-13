// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a lightweight, in-process scenario for testing read model projections and reducers without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Automatically detects how <typeparamref name="TReadModel"/> is projected — either via a reducer
/// (<see cref="IReducerFor{TReadModel}"/>), a fluent projection (<see cref="IProjectionFor{TReadModel}"/>),
/// or a model-bound projection — and routes events through the appropriate engine.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new ReadModelScenario&lt;MyReadModel&gt;();
/// await scenario.Given.ForEventSource(myId).Events(new SomeEvent(), new SomeOtherEvent());
/// scenario.Instance.SomeProperty.ShouldBe(expectedValue);
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TReadModel">The type of read model under test.</typeparam>
/// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
/// <param name="defaults">The <see cref="Defaults"/> to use for service resolution.</param>
/// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reducer and projection instances.</param>
public class ReadModelScenario<TReadModel>(TReadModel? initialState, Defaults defaults, IServiceProvider? serviceProvider)
    where TReadModel : class
{
    readonly TReadModel? _initialState = initialState;
    readonly INamingPolicy _namingPolicy = new CamelCaseNamingPolicy();
    readonly IEventTypes _eventTypes = defaults.EventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator = defaults.JsonSchemaGenerator;
    readonly JsonSerializerOptions _jsonSerializerOptions = Globals.JsonSerializerOptions;
    readonly List<(EventSourceId EventSourceId, object Event)> _collectedEvents = [];
    readonly List<Action<EventStoreForTesting>> _readModelSeeds = [];
    IServiceProvider? _resolvedServiceProvider;
    IClientArtifactsActivator? _artifactsActivator;
    EventStoreForTesting? _eventStore;
    TReadModel? _instance;
    IReadOnlyDictionary<EventSourceId, TReadModel> _instances = new Dictionary<EventSourceId, TReadModel>();
    IReadOnlyList<ReadModelSubstitution>? _substitutions;
    Contracts.Projections.ProjectionDefinition? _projectionDefinition;
    bool _projectionDefinitionResolved;
    bool _processed;
    bool _strictEventSubscription;
    bool _strictFidelity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class.
    /// </summary>
    /// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
    public ReadModelScenario(TReadModel? initialState = null)
        : this(initialState, Defaults.Instance, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class.
    /// </summary>
    /// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reducer and projection instances.</param>
    public ReadModelScenario(TReadModel? initialState, IServiceProvider? serviceProvider)
        : this(initialState, Defaults.Instance, serviceProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class.
    /// </summary>
    /// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
    /// <param name="defaults">The <see cref="Defaults"/> to use for service resolution.</param>
    public ReadModelScenario(TReadModel? initialState, Defaults defaults)
        : this(initialState, defaults, null)
    {
    }

    /// <summary>
    /// Gets the current projected read model instance.
    /// </summary>
    /// <remarks>
    /// This property returns <see langword="null"/> if <see cref="Given"/> has not been used yet or if
    /// the events produced no state changes. Accessing this property triggers event processing the first
    /// time it is called after events have been collected via <see cref="CollectEventsFor"/> or
    /// <see cref="ProcessEventsFor"/>.
    /// When the seeded events materialize more than one instance — for example a multi-source join —
    /// a single result is ambiguous, so this throws <see cref="MultipleInstancesMaterialized"/>; select the
    /// intended instance with <see cref="InstanceForEventSourceId(EventSourceId)"/> or <see cref="Instances"/>.
    /// </remarks>
    /// <exception cref="MultipleInstancesMaterialized">Thrown when more than one instance materialized.</exception>
    public TReadModel? Instance
    {
        get
        {
            EnsureProcessed();
            if (_instances.Count > 1)
            {
                throw new MultipleInstancesMaterialized(typeof(TReadModel), _instances.Keys);
            }

            return _instance;
        }
    }

    /// <summary>
    /// Gets every materialized read model instance, keyed by its event source id.
    /// </summary>
    /// <remarks>
    /// Where <see cref="Instance"/> returns the single instance under test, this reads one document per
    /// resolved root key from the sink. Use it (or <see cref="InstanceForEventSourceId"/>) for multi-source
    /// projections, such as a join whose join-source event was seeded before the entity under test, to
    /// assert against the intended instance deterministically. Each instance carries only what was
    /// projected onto its own key — seeding a second event source never adds to the first. It is populated
    /// for projections; reducers are single-instance and expose their result through <see cref="Instance"/>
    /// only.
    /// </remarks>
    public IReadOnlyDictionary<EventSourceId, TReadModel> Instances
    {
        get
        {
            EnsureProcessed();
            return _instances;
        }
    }

    /// <summary>
    /// Gets the entry point of the fluent builder for seeding events or read model instances into this scenario.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// await scenario.Given
    ///     .ForEventSource(myId)
    ///     .Events(new SomeEvent(), new SomeOtherEvent());
    ///
    /// // Or to pre-seed a read model instance:
    /// scenario.Given
    ///     .ForEventSourceId(myId)
    ///     .ReadModel(new MyReadModel { ... });
    /// </code>
    /// </remarks>
    public ReadModelScenarioGivenBuilder<TReadModel> Given => new(this);

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to resolve reducer and projection instances and their dependencies.
    /// </summary>
    /// <remarks>
    /// Register the dependencies a reducer or projection needs here — for example NSubstitute mocks. When left
    /// untouched (and no <see cref="IServiceProvider"/> was supplied to the constructor), a
    /// <see cref="DefaultServiceProvider"/> is used, preserving the default activation behavior. Registering anything
    /// here builds a standard provider (with logging) from it instead. Ignored when an <see cref="IServiceProvider"/>
    /// was supplied to the constructor.
    /// </remarks>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Gets the layers this harness substitutes that the read model under test actually depends on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Running in-process means standing in for the sink, for storage and the observer lifecycle, for the event
    /// context, for <c>[Join]</c> key resolution and for deferred handling. Most of that applies to every
    /// scenario alike and is described in the testing documentation. This property reports the part that does
    /// not: the substituted layers <typeparamref name="TReadModel"/>'s own shape reaches, so a spec asserting
    /// against behavior that lives in one of them can be recognized as needing a kernel-backed sibling rather
    /// than reading as full coverage. An empty list means nothing shape-dependent is being stood in for.
    /// </para>
    /// <para>
    /// This is derived from the read model and its projection alone, so it is available before any event is
    /// seeded. To have it fail a spec rather than inform one, opt in with <see cref="WithStrictFidelity"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ReadModelSubstitution> Substitutions =>
        _substitutions ??= SubstitutedLayers.DetectFor(
            typeof(TReadModel),
            FindReducerType(typeof(TReadModel)) is null ? ProjectionDefinition() : null);

    /// <summary>
    /// Gets an <see cref="IReadModels"/> instance that returns pre-seeded read model instances for this scenario.
    /// </summary>
    /// <remarks>
    /// Pass this to production code that depends on <see cref="IReadModels"/> to have
    /// <c>GetInstanceById</c> calls return the instances registered via
    /// <c>Given.ForEventSourceId(...).ReadModel(...)</c>.
    /// </remarks>
    public IReadModels ReadModels => EventStore().ReadModels;

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> holding the artifacts Chronicle discovered — the same
    /// registry this scenario resolves its projection or reducer from.
    /// </summary>
    /// <remarks>
    /// Read this instead of reflecting over your own assemblies when a spec needs to know what Chronicle
    /// registered — the event types, projections, reducers, reactors and constraints — including the
    /// classifications the registry draws itself, such as an event type with a property-level
    /// <c>[Unique]</c> landing in <see cref="IClientArtifactsProvider.UniqueConstraints"/> while one with a
    /// class-level <c>[Unique]</c> lands in <see cref="IClientArtifactsProvider.UniqueEventTypeConstraints"/>.
    /// It is read-only: reading it neither triggers nor alters registration, and every read hands out the
    /// same instance — the one from the <see cref="Defaults"/> the scenario was constructed with, which by
    /// default is the process-wide <see cref="Defaults.Instance"/>. The same registry is reachable outside a
    /// scenario as <c>Defaults.Instance.ClientArtifactsProvider</c>.
    /// </remarks>
    public IClientArtifactsProvider ClientArtifactsProvider { get; } = defaults.ClientArtifactsProvider;

    /// <summary>
    /// Enables strict event subscription: seeding an event the projection does not subscribe to raises
    /// <see cref="UnsubscribedEventSeeded"/> instead of being silently skipped.
    /// </summary>
    /// <returns>This <see cref="ReadModelScenario{TReadModel}"/> for chaining.</returns>
    /// <remarks>
    /// By default the scenario mirrors the production projection engine, which filters an event source's
    /// stream to the projection's subscribed types — so a seeded audit/marker event unrelated to the
    /// projection is silently ignored. Opt in to strict mode when a spec wants seeding an unsubscribed event
    /// to fail loudly, e.g. to guard against accidentally seeding the wrong event type. This applies to
    /// projection-backed read models; reducer-backed read models only invoke handlers for the event types
    /// they declare, so an unsubscribed seeded event is inherently a no-op there, exactly as at runtime.
    /// </remarks>
    public ReadModelScenario<TReadModel> WithStrictEventSubscription()
    {
        _strictEventSubscription = true;
        _processed = false;
        return this;
    }

    /// <summary>
    /// Enables strict fidelity: reading a result for a read model that depends on a substituted layer raises
    /// <see cref="ReadModelDependsOnSubstitutedLayer"/> instead of reporting it through <see cref="Substitutions"/>.
    /// </summary>
    /// <returns>This <see cref="ReadModelScenario{TReadModel}"/> for chaining.</returns>
    /// <remarks>
    /// By default a scenario runs whatever shape it is given and reports what it stood in for. Opt in to strict
    /// mode when a suite wants that report to be binding — a read model reaching a substituted layer then cannot
    /// claim a green in-process spec, and has to be covered where the layer is real. Because the check reads the
    /// read model's shape rather than the events, it fires whether or not anything was seeded.
    /// </remarks>
    public ReadModelScenario<TReadModel> WithStrictFidelity()
    {
        _strictFidelity = true;
        return this;
    }

    /// <summary>
    /// Gets the materialized read model instance for a specific event source id.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the instance to return.</param>
    /// <returns>The instance for the given event source id, or <see langword="null"/> if none was materialized.</returns>
    /// <remarks>
    /// This resolves a specific instance from the sink regardless of seed order, so a join/cross-stream
    /// spec can assert against the intended entity even when another source's event was seeded first.
    /// </remarks>
    public TReadModel? InstanceForEventSourceId(EventSourceId eventSourceId)
    {
        EnsureProcessed();
        return _instances.TryGetValue(eventSourceId, out var instance) ? instance : null;
    }

    /// <summary>
    /// Registers a pre-built read model instance for a specific event source, making it available via
    /// <see cref="ReadModels"/> for calls to <c>GetInstanceById</c>.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the read model instance with.</param>
    /// <param name="readModel">The read model instance to register.</param>
    public void CollectReadModelFor(EventSourceId eventSourceId, TReadModel readModel)
    {
        if (_eventStore is not null)
        {
            _eventStore.RegisterReadModelInstance(eventSourceId, readModel);
            return;
        }

        _readModelSeeds.Add(store => store.RegisterReadModelInstance(eventSourceId, readModel));
    }

    /// <summary>
    /// Collects events for a specific event source to be processed together when <see cref="Instance"/> is accessed.
    /// </summary>
    /// <remarks>
    /// This method accumulates events without immediately processing them. Processing is deferred until
    /// <see cref="Instance"/> is first accessed, allowing events across multiple event sources to be
    /// collected and then processed together. This is required for projections that use
    /// <c>ChildrenFrom</c> with events on separate event source streams.
    /// If events are collected after <see cref="Instance"/> has already been accessed, the next access
    /// to <see cref="Instance"/> will re-process all collected events including the newly added ones.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to collect in order.</param>
    public void CollectEventsFor(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        _processed = false;
        foreach (var @event in events)
        {
            _collectedEvents.Add((eventSourceId, @event));
        }
    }

    /// <summary>
    /// Feeds the provided events for a specific event source through the read model's projection or reducer and updates <see cref="Instance"/>.
    /// </summary>
    /// <remarks>
    /// Events are accumulated and processed together with all previously collected events when
    /// <see cref="Instance"/> is next accessed. This enables multi-stream scenarios where events across
    /// different event sources are needed for hierarchical projections.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to process in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task ProcessEventsFor(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        CollectEventsFor(eventSourceId, events);
        return Task.CompletedTask;
    }

    IServiceProvider ResolvedServiceProvider() => _resolvedServiceProvider ??= serviceProvider ?? BuildServiceProvider();

#pragma warning disable CA2000 // Dispose objects before losing scope — held for the scenario's lifetime
    IClientArtifactsActivator ArtifactsActivator() =>
        _artifactsActivator ??= new ClientArtifactsActivator(ResolvedServiceProvider(), new NullLoggerFactory());

    EventStoreForTesting EventStore()
    {
        if (_eventStore is null)
        {
            _eventStore = new EventStoreForTesting(ResolvedServiceProvider());
            foreach (var seed in _readModelSeeds)
            {
                seed(_eventStore);
            }
        }

        return _eventStore;
    }

    IServiceProvider BuildServiceProvider()
    {
        if (!Services.Any())
        {
            return new DefaultServiceProvider();
        }

        Services.AddLogging();
        return Services.BuildServiceProvider();
    }
#pragma warning restore CA2000 // Dispose objects before losing scope

    Contracts.Projections.ProjectionDefinition? ProjectionDefinition()
    {
        if (!_projectionDefinitionResolved)
        {
            _projectionDefinition = FindProjectionDefinition(typeof(TReadModel));
            _projectionDefinitionResolved = true;
        }

        return _projectionDefinition;
    }

    void EnsureProcessed()
    {
        if (_strictFidelity && Substitutions.Count > 0)
        {
            throw new ReadModelDependsOnSubstitutedLayer(typeof(TReadModel), Substitutions);
        }

        if (!_processed && _collectedEvents.Count > 0)
        {
            (_instance, _instances) = ProcessEvents(_collectedEvents).GetAwaiter().GetResult();
            _processed = true;
        }
    }

    async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> ProcessEvents(IEnumerable<(EventSourceId EventSourceId, object Event)> events)
    {
        var eventsList = events.ToList();
        var readModelType = typeof(TReadModel);

        var reducerType = FindReducerType(readModelType);
        if (reducerType is not null)
        {
            var reduced = await ReducerReadModelProcessor.Process<TReadModel>(
                reducerType,
                eventsList.Select(e => new EventForEventSourceId(e.EventSourceId, e.Event, Causation.Unknown())),
                _eventTypes,
                ArtifactsActivator(),
                ResolvedServiceProvider(),
                _namingPolicy,
                _initialState);

            // A reducer is single-instance; key its result by the event source id it reduced so that
            // InstanceForEventSourceId / Instances behave the same as for a projection. Only the single-source
            // case is keyed — reducing events across more than one source is misuse of a single-instance API.
            var reducerInstances = new Dictionary<EventSourceId, TReadModel>();
            var distinctEventSourceIds = eventsList.Select(e => e.EventSourceId).Distinct().ToList();
            if (reduced is not null && distinctEventSourceIds.Count == 1)
            {
                reducerInstances[distinctEventSourceIds[0]] = reduced;
            }

            return (reduced, reducerInstances);
        }

        var projectionDefinition = ProjectionDefinition();
        if (projectionDefinition is not null)
        {
            return await ProjectionReadModelProcessor.Process(
                projectionDefinition,
                eventsList,
                _eventTypes,
                _jsonSchemaGenerator,
                _initialState,
                _strictEventSubscription);
        }

        throw new NoReadModelHandlerFound(readModelType);
    }

    Type? FindReducerType(Type readModelType) =>
        ClientArtifactsProvider.Reducers.FirstOrDefault(t =>
            t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IReducerFor<>) &&
                i.GetGenericArguments()[0] == readModelType));

    Contracts.Projections.ProjectionDefinition? FindProjectionDefinition(Type readModelType)
    {
        // Try fluent IProjectionFor<TReadModel> projection
        var projectionType = ClientArtifactsProvider.Projections.FirstOrDefault(t =>
            t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IProjectionFor<>) &&
                i.GetGenericArguments()[0] == readModelType));

        if (projectionType is not null)
        {
            return BuildFluentProjectionDefinition(projectionType, readModelType);
        }

        // Try model-bound projection directly on TReadModel
        if (readModelType.HasModelBoundProjectionAttributes())
        {
            var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);
            return builder.Build(readModelType);
        }

        // Try model-bound projection for a type in clientArtifacts that matches
        var modelBoundType = ClientArtifactsProvider.ModelBoundProjections
            .FirstOrDefault(t => t == readModelType);

        if (modelBoundType is not null)
        {
            var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);
            return builder.Build(modelBoundType);
        }

        return null;
    }

    Contracts.Projections.ProjectionDefinition? BuildFluentProjectionDefinition(Type projectionType, Type readModelType)
    {
        var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(readModelType);
        var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<TReadModel>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)
            ?? throw new ProjectionDefinitionBuildFailed(projectionType, new InvalidOperationException("CreateAndDefine method not found on ProjectionDefinitionCreator."));

        var result = (Monads.Catch<Contracts.Projections.ProjectionDefinition>)method.Invoke(
            null,
            [
                projectionType,
                _namingPolicy,
                _eventTypes,
                ArtifactsActivator(),
                _jsonSerializerOptions
            ])!;

        if (result.TryGetException(out var exception))
        {
            throw new ProjectionDefinitionBuildFailed(projectionType, exception);
        }

        return result.AsT0;
    }
}
