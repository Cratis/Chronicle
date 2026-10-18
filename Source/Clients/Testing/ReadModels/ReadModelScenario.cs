// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
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
/// <see cref="WithProjection"/> overrides auto-detection for this scenario, including a discovered reducer.
/// </para>
/// <para>
/// Usage:
/// <code language="csharp">
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
public class ReadModelScenario<TReadModel>(TReadModel? initialState, Defaults defaults, IServiceProvider? serviceProvider) : IDisposable
    where TReadModel : class
{
    readonly TReadModel? _initialState = initialState;
    readonly INamingPolicy _namingPolicy = new CamelCaseNamingPolicy();
    readonly IEventTypes _eventTypes = defaults.EventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator = defaults.JsonSchemaGenerator;
    readonly JsonSerializerOptions _jsonSerializerOptions = Globals.JsonSerializerOptions;
    readonly List<ReadModelEvent> _collectedEvents = [];
    readonly Dictionary<EventSourceId, TReadModel> _readModelSeeds = [];
    IServiceProvider? _resolvedServiceProvider;
    IClientArtifactsActivator? _artifactsActivator;
    EventStoreForTesting? _eventStore;
    TReadModel? _instance;
    IReadOnlyDictionary<EventSourceId, TReadModel> _instances = new Dictionary<EventSourceId, TReadModel>();
    IReadOnlyList<ReadModelSubstitution>? _substitutions;
    Contracts.Projections.ProjectionDefinition? _projectionDefinition;
    bool _projectionDefinitionResolved;
    bool _inlineProjection;
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
    /// This property returns <see langword="null"/> if no instance was seeded and the events produced no state changes. Accessing this property triggers event processing the first
    /// time it is called after events have been collected via <see cref="CollectEventsFor(EventSourceId, IEnumerable{object})"/> or
    /// <see cref="ProcessEventsFor"/> or <see cref="ProcessAppendedEvents"/>.
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
    /// for projections and for reducers fed through <see cref="ProcessAppendedEvents"/>. Reducers fed only
    /// bare events retain their single-instance behavior unless per-key read model instances were seeded.
    /// A seeded projection or reducer instance is included even when no event addresses its key.
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
    /// <code language="csharp">
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
    /// context, for <c language="csharp">[Join]</c> key resolution and for deferred handling. Most of that applies to every
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
    public IReadOnlyList<ReadModelSubstitution> Substitutions => _substitutions ??= DetectSubstitutions();

    /// <summary>
    /// Gets an <see cref="IReadModels"/> instance that returns this scenario's seeded and folded read model instances.
    /// </summary>
    /// <remarks>
    /// Pass this to production code that depends on <see cref="IReadModels"/> to have
    /// <c language="csharp">GetInstanceById</c> calls return the instances registered via
    /// <c language="csharp">Given.ForEventSourceId(...).ReadModel(...)</c>.
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
    /// <c language="csharp">[Unique]</c> landing in <see cref="IClientArtifactsProvider.UniqueConstraints"/> while one with a
    /// class-level <c language="csharp">[Unique]</c> lands in <see cref="IClientArtifactsProvider.UniqueEventTypeConstraints"/>.
    /// It is read-only: reading it neither triggers nor alters registration, and every read hands out the
    /// same instance — the one from the <see cref="Defaults"/> the scenario was constructed with. For the
    /// default constructors, it is the process-wide <see cref="Defaults.Instance"/> registry, also reachable
    /// outside a scenario as <c language="csharp">Defaults.Instance.ClientArtifactsProvider</c>.
    /// </remarks>
    public IClientArtifactsProvider ClientArtifactsProvider { get; } = defaults.ClientArtifactsProvider;

    /// <summary>
    /// Defines a projection for this scenario only, instead of discovering a projection or reducer.
    /// </summary>
    /// <param name="define">Configures the projection for <typeparamref name="TReadModel"/>.</param>
    /// <returns>This scenario for chaining.</returns>
    public ReadModelScenario<TReadModel> WithProjection(Action<IProjectionBuilderFor<TReadModel>> define)
    {
        ArgumentNullException.ThrowIfNull(define);

        var builder = new ProjectionBuilderFor<TReadModel>(
            Guid.NewGuid().ToString(), typeof(TReadModel), _namingPolicy, _eventTypes, _jsonSerializerOptions);
        define(builder);
        _projectionDefinition = builder.Build();
        _projectionDefinitionResolved = true;
        _inlineProjection = true;
        _substitutions = null;
        _processed = false;
        return this;
    }

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
    /// Seeds the fold for a specific read-model key and makes it available via <see cref="ReadModels"/>.
    /// Models without a projection or reducer are registered for keyed lookups only.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the read model instance with.</param>
    /// <param name="readModel">The read model instance to register.</param>
    /// <exception cref="ReadModelAlreadySeeded">Thrown when this key was already seeded.</exception>
    public void CollectReadModelFor(EventSourceId eventSourceId, TReadModel readModel)
    {
        if (!_readModelSeeds.TryAdd(eventSourceId, readModel))
        {
            throw new ReadModelAlreadySeeded(typeof(TReadModel), eventSourceId);
        }

        _processed = false;
        _eventStore?.RegisterReadModelInstance(eventSourceId, readModel);
    }

    /// <summary>
    /// Collects events for a specific event source to be processed together when <see cref="Instance"/> is accessed.
    /// </summary>
    /// <remarks>
    /// This method accumulates events without immediately processing them. Processing is deferred until
    /// <see cref="Instance"/> is first accessed, allowing events across multiple event sources to be
    /// collected and then processed together. This is required for projections that use
    /// <c language="csharp">ChildrenFrom</c> with events on separate event source streams.
    /// If events are collected after <see cref="Instance"/> has already been accessed, the next access
    /// to <see cref="Instance"/> will re-process all collected events including the newly added ones.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to collect in order.</param>
    public void CollectEventsFor(EventSourceId eventSourceId, IEnumerable<object> events) =>
        CollectEventsFor(eventSourceId, events, EventSourceType.Default, EventStreamType.All, EventStreamId.Default);

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

    /// <summary>
    /// Feeds appended events through the read model's projection or reducer, preserving their original contexts.
    /// </summary>
    /// <param name="events">The appended events, potentially from several event sources.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Events accumulate with previously collected input and are processed when a result is accessed.
    /// Appended events are sorted by their original sequence numbers. Use distinct sequence numbers.
    /// Bare events collected through <see cref="ProcessEventsFor"/> or <see cref="Given"/> keep synthetic
    /// contexts with collection-index sequence numbers. Projections use their existing key resolution;
    /// reducers fold each event source independently when appended input is present.
    /// </remarks>
    public Task ProcessAppendedEvents(IEnumerable<AppendedEvent> events)
    {
        _processed = false;
        foreach (var @event in events)
        {
            var context = @event.Context;
            _collectedEvents.Add(new(context.EventSourceId, @event.Content, context.EventSourceType, context.EventStreamType, context.EventStreamId, context.Tags.Select(tag => tag.Value).ToArray())
            {
                Context = context
            });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _eventStore?.Dispose();
        if (serviceProvider is null && Services.Any() && _resolvedServiceProvider is IDisposable disposable) disposable.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Collects events together with their source and stream route.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="events">The event instances to collect in order.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="tags">Append tags to combine with static event tags.</param>
    internal void CollectEventsFor(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        IEnumerable<string>? tags = null)
    {
        var appendTags = tags?.ToArray() ?? [];
        _processed = false;
        foreach (var @event in events)
        {
            _collectedEvents.Add(new(eventSourceId, @event, eventSourceType, eventStreamType, eventStreamId, @event.GetType().GetTags().Concat(appendTags).Distinct(StringComparer.Ordinal).ToArray()));
        }
    }

    IServiceProvider ResolvedServiceProvider() => _resolvedServiceProvider ??= serviceProvider ?? BuildServiceProvider();

#pragma warning disable CA2000 // Dispose objects before losing scope — held for the scenario's lifetime
    IClientArtifactsActivator ArtifactsActivator() =>
        _artifactsActivator ??= new ClientArtifactsActivator(ResolvedServiceProvider(), new NullLoggerFactory());

    EventStoreForTesting EventStore()
    {
        if (_eventStore is null)
        {
            _eventStore = new EventStoreForTesting(ResolvedServiceProvider(), defaults.ClientArtifactsProvider, defaults.ScenarioOptions, testingStore: null, eventLog: null, eventStore: defaults.EventStore.Name, @namespace: defaults.EventStore.Namespace);
            ((ReadModelsForTesting)_eventStore.ReadModels).DisableObservation();
            ((ReadModelsForTesting)_eventStore.ReadModels).BeforeInstanceRead = () =>
            {
                if (_readModelSeeds.Count > 0) EnsureProcessed();
            };
            foreach (var (key, seed) in _readModelSeeds)
            {
                _eventStore.RegisterReadModelInstance(key, seed);
            }
        }

        return _eventStore;
    }

    IServiceProvider BuildServiceProvider()
    {
        if (!Services.Any())
        {
            return ((EventStoreForTesting)defaults.EventStore).ServiceProvider;
        }

        Services.AddLogging();
        return Services.BuildServiceProvider();
    }
#pragma warning restore CA2000 // Dispose objects before losing scope

    IReadOnlyList<ReadModelSubstitution> DetectSubstitutions()
    {
        var isReduced = !_inlineProjection && FindReducerType(typeof(TReadModel)) is not null;
        return SubstitutedLayers.DetectFor(
            typeof(TReadModel),
            isReduced ? null : ProjectionDefinition(),
            _jsonSchemaGenerator.GenerateForReadModel(typeof(TReadModel)),
            isReduced ? ReducerReadModelProcessor.AppliesCompliance : ProjectionReadModelProcessor.AppliesCompliance);
    }

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

        if (_readModelSeeds.Count > 0 && FindReducerType(typeof(TReadModel)) is null && ProjectionDefinition() is null)
        {
            return;
        }

        if (!_processed && (_collectedEvents.Count > 0 || _readModelSeeds.Count > 0))
        {
            (_instance, _instances) = ProcessEvents(_collectedEvents).GetAwaiter().GetResult();
            if (_readModelSeeds.Count > 0) ((ReadModelsForTesting)EventStore().ReadModels).ReplaceInstances(_instances);
            _processed = true;
        }
    }

    Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> ProcessEvents(IEnumerable<ReadModelEvent> events)
    {
        var reducerType = _inlineProjection ? null : FindReducerType(typeof(TReadModel));
        var fold = new ReadModelFold<TReadModel>
        {
            EventStore = EventStore(),
            EventTypes = _eventTypes,
            SchemaGenerator = _jsonSchemaGenerator,
            ServiceProvider = ResolvedServiceProvider(),
            ReducerType = reducerType,
            ProjectionDefinition = reducerType is null ? ProjectionDefinition() : null,
            InitialState = _initialState,
            SeededInstances = _readModelSeeds,
            StrictEventSubscription = _strictEventSubscription
        };
        return fold.Process(events);
    }

    Type? FindReducerType(Type readModelType) =>
        EventStore().ExplicitSnapshot.Projections.Any(_ => _.ReadModelType == readModelType) ? null :
        EventStore().Reducers.GetAllHandlers().FirstOrDefault(_ => _.ReadModelType == readModelType)?.ReducerType;

    Contracts.Projections.ProjectionDefinition? FindProjectionDefinition(Type readModelType)
    {
        var explicitProjection = EventStore().ExplicitSnapshot.Projections.FirstOrDefault(_ => _.ReadModelType == readModelType);
        if (explicitProjection is not null)
        {
            return ((Projections.Projections)EventStore().Projections).Definitions
                .FirstOrDefault(_ => _.Identifier == explicitProjection.Id.Value);
        }

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
            return BuildModelBoundDefinition(readModelType);
        }

        // Try model-bound projection for a type in clientArtifacts that matches
        var modelBoundType = ClientArtifactsProvider.ModelBoundProjections
            .FirstOrDefault(t => t == readModelType);

        if (modelBoundType is not null)
        {
            return BuildModelBoundDefinition(modelBoundType);
        }

        return null;
    }

    /// <summary>
    /// Builds a model-bound <see cref="Contracts.Projections.ProjectionDefinition"/>, routing a
    /// <c language="csharp">[VariantOf&lt;T&gt;]</c> type through the same variant-aware build the real discovery
    /// pipeline uses.
    /// </summary>
    /// <remarks>
    /// A plain <c language="csharp">Build(modelType)</c> only reclassifies what a single type declares — it
    /// does not know a variant's siblings, so it would neither move non-entering handlers to an update-only
    /// join nor wire the mutual-exclusion <c language="csharp">RemovedWith</c> entries. Without this, a scenario
    /// for a variant read model would silently prove nothing about variant behavior: it would look like an
    /// ordinary upsert projection. Siblings and shared handlers are discovered the same way
    /// <see cref="Cratis.Chronicle.Projections.ModelBound.ModelBoundProjections"/> does, from every model-bound
    /// type the client artifacts provider knows about — which includes every type in this test assembly, not
    /// just <typeparamref name="TReadModel"/>.
    /// </remarks>
    /// <param name="modelType">The model-bound read model type to build a definition for.</param>
    /// <returns>The built <see cref="Contracts.Projections.ProjectionDefinition"/>.</returns>
    Contracts.Projections.ProjectionDefinition BuildModelBoundDefinition(Type modelType)
    {
        var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);

        if (!modelType.TryGetVariantIdentity(out var identity))
        {
            return builder.Build(modelType);
        }

        var siblingVariantTypes = ClientArtifactsProvider.ModelBoundProjections
            .Where(candidate => candidate != modelType &&
                                candidate.TryGetVariantIdentity(out var candidateIdentity) &&
                                candidateIdentity == identity)
            .ToList();

        var globalHandlerTypes = ClientArtifactsProvider.ModelBoundProjections
            .Where(candidate => candidate.TryGetGlobalForIdentity(out var candidateIdentity) && candidateIdentity == identity)
            .ToList();

        return builder.BuildVariant(modelType, siblingVariantTypes, globalHandlerTypes);
    }

    Contracts.Projections.ProjectionDefinition? BuildFluentProjectionDefinition(Type projectionType, Type readModelType)
    {
        var variantDeclarations = new Dictionary<Type, FluentVariantDeclaration>();
        var definition = CreateFluentDefinition(projectionType, readModelType, variantDeclarations);

        if (!variantDeclarations.TryGetValue(projectionType, out var declaration))
        {
            return definition;
        }

        // A fluent variant is told what removes it only once its siblings are known, which discovery does after
        // building every projection. Do the same for the group this projection belongs to, so entering the variant
        // moves the entity out of the others here as it does under a running kernel.
        var definitions = new Dictionary<Type, Contracts.Projections.ProjectionDefinition> { [projectionType] = definition };
        foreach (var siblingType in ClientArtifactsProvider.Projections.Where(candidate => candidate != projectionType))
        {
            var siblingReadModelType = siblingType.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0];
            var siblingDeclarations = new Dictionary<Type, FluentVariantDeclaration>();
            var sibling = CreateFluentDefinition(siblingType, siblingReadModelType, siblingDeclarations);
            if (siblingDeclarations.TryGetValue(siblingType, out var siblingDeclaration) && siblingDeclaration.Identity == declaration.Identity)
            {
                variantDeclarations[siblingType] = siblingDeclaration;
                definitions[siblingType] = sibling;
            }
        }

        VariantReclassifier.CrossWireGroups(definitions, variantDeclarations);
        return definition;
    }

    Contracts.Projections.ProjectionDefinition CreateFluentDefinition(Type projectionType, Type readModelType, IDictionary<Type, FluentVariantDeclaration> variantDeclarations)
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
                _jsonSerializerOptions,
                variantDeclarations
            ])!;

        if (result.TryGetException(out var exception))
        {
            throw new ProjectionDefinitionBuildFailed(projectionType, exception);
        }

        return result.AsT0;
    }
}
