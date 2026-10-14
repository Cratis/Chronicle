// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Testing.Events;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IReactor"/> implementations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Activates a fresh instance of <typeparamref name="TReactor"/> from the scenario's <see cref="Services"/> (or a
/// supplied <see cref="IServiceProvider"/>) and routes events directly through the <see cref="ReactorInvoker"/> — no
/// Chronicle server, gRPC, or observer registration required. The reactor's constructor dependencies and any
/// service-typed handler-method parameters are resolved from that provider; read-model handler parameters are
/// resolved from read models seeded via <c language="csharp">Given.ForEventSourceId(...).ReadModel(...)</c>.
/// Given events do not build read models, including passive read models. An unseeded read-model parameter throws
/// <see cref="ReadModelNotSeededForReactorScenario"/> unless absence was declared with
/// <c language="csharp">Given.ForEventSourceId(...).NoReadModel&lt;TReadModel&gt;()</c>. This guard does not apply
/// when an explicit event store other than <see cref="EventStoreForTesting"/> is supplied.
/// </para>
/// <para>
/// Usage:
/// <code language="csharp">
/// var scenario = new ReactorScenario&lt;MyReactor&gt;();
/// scenario.Services.AddSingleton(_someService);
/// scenario.Given.ForEventSourceId(myId).ReadModel(new MyReadModel(...));
/// await scenario.Given.ForEventSource(myId).Events(new SomeEvent(), new AnotherEvent());
/// // Assert on side-effects captured by the mocks registered in Services
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
public class ReactorScenario<TReactor> : IDisposable
    where TReactor : class
{
    readonly Defaults _defaults;
    readonly IReactorSideEffectHandlers? _sideEffectHandlers;
    readonly IEventStore? _explicitEventStore;
    readonly IServiceProvider? _explicitServiceProvider;
    readonly IEventTypes _eventTypes;
    readonly List<Action<EventStoreForTesting>> _readModelSeeds = [];
    readonly HashSet<(Type ReadModelType, string Key)> _absentReadModels = [];
    readonly RecordingReactorSideEffectHandlers _recordingHandlers;
    readonly ScenarioOccurrenceRecorder _occurrences;
    IServiceProvider? _serviceProvider;
    IEventStore? _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorScenario{TReactor}"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// Optional <see cref="IServiceProvider"/> for resolving the reactor and its dependencies. When
    /// <see langword="null"/>, the scenario builds one from <see cref="Services"/> (which already has logging
    /// registered). Supply this only when a pre-built provider is required; otherwise prefer <see cref="Services"/>.
    /// </param>
    /// <param name="sideEffectHandlers">
    /// Optional <see cref="IReactorSideEffectHandlers"/> for processing events returned as side effects by handler methods.
    /// When <see langword="null"/>, returned events are silently discarded.
    /// </param>
    /// <param name="eventStore">
    /// Optional <see cref="IEventStore"/> passed to side effect handlers when they append events and used to materialize
    /// read-model handler parameters. When <see langword="null"/>, the scenario provides its own in-process event store
    /// so read-model seeding works out of the box.
    /// </param>
    public ReactorScenario(
        IServiceProvider? serviceProvider = null,
        IReactorSideEffectHandlers? sideEffectHandlers = null,
        IEventStore? eventStore = null)
        : this(Defaults.Instance, serviceProvider, sideEffectHandlers, eventStore)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorScenario{TReactor}"/> class with per-run defaults.
    /// </summary>
    /// <param name="defaults">The defaults used for event types and artifact discovery.</param>
    /// <param name="serviceProvider">Optional provider for resolving the reactor and its dependencies.</param>
    /// <param name="sideEffectHandlers">Optional handlers for returned side effects.</param>
    /// <param name="eventStore">Optional event store passed to side effect handlers and used for read-model seeding.</param>
    public ReactorScenario(
        Defaults defaults,
        IServiceProvider? serviceProvider = null,
        IReactorSideEffectHandlers? sideEffectHandlers = null,
        IEventStore? eventStore = null)
    {
        _defaults = defaults;
        if (!typeof(TReactor).IsAssignableTo(typeof(IReactor)) &&
            ExplicitArtifactCatalog.From(defaults.ClientArtifactsProvider)?.IsExplicit(typeof(TReactor), ExplicitArtifactKind.Reactor) != true)
        {
            throw new InvalidExplicitArtifact(typeof(TReactor), "reactor", "A plain reactor scenario requires explicit registration in its per-run Defaults.");
        }
        _eventTypes = defaults.EventTypes;
        _occurrences = new(_eventTypes, "testing", "default");
        _explicitServiceProvider = serviceProvider;
        _sideEffectHandlers = sideEffectHandlers;
        _explicitEventStore = eventStore;
        Services = new ServiceCollection();
        Services.AddLogging();
        _recordingHandlers = new(type => EnsureServiceProvider()
            .GetServices<IReactorSideEffectHandler>()
            .Any(handler => handler.CanHandleReturnType(type)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorScenario{TReactor}"/> class and configures its <see cref="Services"/>.
    /// </summary>
    /// <param name="configureServices">A callback for registering the reactor's dependencies into <see cref="Services"/>.</param>
    public ReactorScenario(Action<IServiceCollection> configureServices)
        : this() =>
        configureServices(Services);

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to resolve the reactor and its dependencies.
    /// </summary>
    /// <remarks>
    /// Register the reactor's constructor dependencies and any service-typed handler-method parameters here — for
    /// example NSubstitute mocks. Logging is registered by default. This is ignored when an <see cref="IServiceProvider"/>
    /// was supplied to the constructor.
    /// </remarks>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the entry point of the fluent builder for providing events to, and seeding read models for, the reactor.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code language="csharp">
    /// scenario.Given.ForEventSourceId(myId).ReadModel(new MyReadModel(...));
    /// await scenario.Given.ForEventSource(myId).Events(new SomeEvent());
    /// </code>
    /// </remarks>
    public ReactorScenarioGivenBuilder<TReactor> Given => new(this);

    /// <summary>
    /// Gets the side effects the reactor produced — the events and commands it returned from its handler methods —
    /// flattened, with cross-stream event wrappers unwrapped to the underlying event.
    /// </summary>
    /// <remarks>
    /// Populated only when no explicit <see cref="IReactorSideEffectHandlers"/> was supplied to the constructor; with
    /// explicit handlers, assert against those instead.
    /// </remarks>
    public IReadOnlyList<object> Produced => _recordingHandlers.Produced;

    /// <summary>
    /// Gets the produced events together with their resolved destinations, excluding commands and other non-event side effects.
    /// </summary>
    /// <remarks>
    /// Events are recorded rather than appended. Populated only when no explicit
    /// <see cref="IReactorSideEffectHandlers"/> was supplied to the constructor.
    /// </remarks>
    public IReadOnlyList<ProducedEvent> ProducedEvents => _recordingHandlers.ProducedEvents;

    /// <summary>
    /// Asserts that the reactor produced at least one side effect of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the produced event or command to assert.</typeparam>
    /// <exception cref="ReactorSideEffectAssertionException">Thrown when no produced side effect is of type <typeparamref name="T"/>.</exception>
    public void ShouldHaveProduced<T>() => ShouldHaveProduced<T>(_ => true);

    /// <summary>
    /// Asserts that the reactor produced at least one side effect of type <typeparamref name="T"/> matching the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the produced event or command to assert.</typeparam>
    /// <param name="predicate">The predicate the produced side effect must satisfy.</param>
    /// <exception cref="ReactorSideEffectAssertionException">Thrown when no produced side effect of type <typeparamref name="T"/> matches.</exception>
    public void ShouldHaveProduced<T>(Func<T, bool> predicate)
    {
        if (!_recordingHandlers.Produced.OfType<T>().Any(predicate))
        {
            var produced = _recordingHandlers.Produced.Count > 0
                ? string.Join(", ", _recordingHandlers.Produced.Select(_ => _.GetType().Name))
                : "nothing";
            throw new ReactorSideEffectAssertionException(
                $"Expected reactor '{typeof(TReactor).Name}' to produce a matching '{typeof(T).Name}', but it produced: {produced}.");
        }
    }

    /// <summary>
    /// Asserts that the reactor produced an event of type <typeparamref name="TEvent"/> for the specified event source.
    /// </summary>
    /// <typeparam name="TEvent">The type of the produced event.</typeparam>
    /// <param name="eventSourceId">The destination event source identifier.</param>
    /// <exception cref="ReactorSideEffectAssertionException">Thrown when no produced event of the type has the specified destination.</exception>
    public void ShouldHaveProduced<TEvent>(EventSourceId eventSourceId) =>
        AssertProduced<TEvent>((produced, _) => produced.EventSourceId == eventSourceId, $"for event source '{eventSourceId.Value}'");

    /// <summary>
    /// Asserts that the reactor produced an event of type <typeparamref name="TEvent"/> matching an event and destination predicate.
    /// </summary>
    /// <typeparam name="TEvent">The type of the produced event.</typeparam>
    /// <param name="predicate">The predicate receiving the recorded destination and the typed event.</param>
    /// <exception cref="ReactorSideEffectAssertionException">Thrown when no produced event of the type satisfies the predicate.</exception>
    public void ShouldHaveProduced<TEvent>(Func<ProducedEvent, TEvent, bool> predicate) =>
        AssertProduced(predicate, "matching the event and destination predicate");

    /// <summary>
    /// Asserts that the reactor produced no side effect of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the produced event or command that must not be present.</typeparam>
    /// <exception cref="ReactorSideEffectAssertionException">Thrown when a produced side effect is of type <typeparamref name="T"/>.</exception>
    public void ShouldNotHaveProduced<T>()
    {
        if (_recordingHandlers.Produced.OfType<T>().Any())
        {
            throw new ReactorSideEffectAssertionException(
                $"Expected reactor '{typeof(TReactor).Name}' to produce no '{typeof(T).Name}', but it did.");
        }
    }

    /// <summary>
    /// Delivers a recorded occurrence with its original context, without appending it or allocating a new sequence number.
    /// </summary>
    /// <param name="occurrence">The event and context to deliver.</param>
    /// <returns>A task completing after the matching handler completes.</returns>
    /// <exception cref="CannotActivateReactorForScenario">Thrown when the reactor's dependencies cannot be resolved.</exception>
    /// <exception cref="ReadModelNotSeededForReactorScenario">Thrown when a read-model parameter was neither seeded nor declared absent.</exception>
    public Task Redeliver(RecordedOccurrence occurrence) => Deliver([occurrence]);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_explicitEventStore is null && _eventStore is IDisposable store) store.Dispose();
        if (_explicitServiceProvider is null && _serviceProvider is IDisposable provider) provider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Seeds a read model for resolving reactor dependencies.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="readModel">The seeded read model.</param>
    internal void SeedReadModel<TReadModel>(EventSourceId eventSourceId, TReadModel readModel)
        where TReadModel : class =>
        _readModelSeeds.Add(store => store.RegisterReadModelInstance(eventSourceId, readModel));

    /// <summary>
    /// Declares that a read model is deliberately absent for a key.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="eventSourceId">The key for which absence is expected.</param>
    internal void DeclareNoReadModel<TReadModel>(EventSourceId eventSourceId)
        where TReadModel : class =>
        _absentReadModels.Add((typeof(TReadModel), eventSourceId.Value));

    /// <summary>
    /// Invokes the reactor with the provided events for a specific event source.
    /// </summary>
    /// <remarks>
    /// A fresh instance of <typeparamref name="TReactor"/> is activated from the service provider for each invocation,
    /// matching the production behavior where a new scope is created per event batch.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to invoke the reactor with, in order.</param>
    /// <param name="eventSourceType">The event source type for the input events.</param>
    /// <param name="eventStreamType">The event stream type for the input events.</param>
    /// <param name="eventStreamId">The event stream identifier for the input events.</param>
    /// <returns>A <see cref="Task"/> that completes when all matching events have been handled.</returns>
    /// <exception cref="CannotActivateReactorForScenario">Thrown when the reactor's dependencies cannot be resolved.</exception>
    /// <exception cref="ReadModelNotSeededForReactorScenario">Thrown when a read-model parameter was neither seeded nor declared absent.</exception>
    internal Task InvokeWith(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId) =>
        Deliver(_occurrences.Record(eventSourceId, events, eventSourceType, eventStreamType, eventStreamId));

    /// <summary>
    /// Allocates original contexts for history without invoking the reactor.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="events">The events to record in order.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <returns>The recorded occurrences.</returns>
    internal Task<IReadOnlyList<RecordedOccurrence>> RecordHistory(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId) =>
        Task.FromResult<IReadOnlyList<RecordedOccurrence>>(_occurrences.Record(eventSourceId, events, eventSourceType, eventStreamType, eventStreamId).ToArray());

    async Task Deliver(IEnumerable<RecordedOccurrence> occurrences)
    {
        var serviceProvider = EnsureServiceProvider();
        var eventStore = EnsureEventStore(serviceProvider);
        var filters = new ObserverRouteFilter(typeof(TReactor), eventStore.EventSources);

        using var loggerFactory = new NullLoggerFactory();
#pragma warning disable CA2000 // Dispose objects before losing scope
        var artifactActivator = new ClientArtifactsActivator(serviceProvider, loggerFactory);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var activationResult = artifactActivator.Activate(serviceProvider, typeof(TReactor));
        if (activationResult.TryGetException(out var exception))
        {
            throw new CannotActivateReactorForScenario(typeof(TReactor), exception);
        }

        await using var activatedReactor = activationResult.AsT0;

        var invoker = new ReactorInvoker(
            _eventTypes,
#pragma warning disable CA2000 // Dispose objects before losing scope — ownership transfers to ReactorInvoker
            new ReactorMiddlewares([]),
#pragma warning restore CA2000
            typeof(TReactor),
            activatedReactor,
            NullLogger<ReactorInvoker>.Instance,
            _sideEffectHandlers ?? _recordingHandlers,
            eventStore,
            new ReactorContextValuesBuilder(new KnownInstancesOf<IReactorContextValuesProvider>(
            [
                new EventSourceIdValuesProvider(),
                new EventStreamIdValuesProvider(),
                new EventStreamTypeValuesProvider(),
                new EventSourceTypeValuesProvider(),
                new SubjectValuesProvider()
            ])),
            argumentsResolver: new ScenarioReactorMethodArgumentsResolver(new ReactorMethodArgumentsResolver(), _absentReadModels),
            serviceProvider: serviceProvider)
        {
            RegisteredId = RegisteredObserverMetadata.ReactorIdFor(_defaults.ClientArtifactsProvider, typeof(TReactor)),
            RegisteredEventSequenceId = typeof(TReactor).GetEventSequenceId(_eventTypes, eventStore.Name, _sideEffectHandlers ?? _recordingHandlers)
        };

        foreach (var occurrence in occurrences)
        {
            var context = occurrence.Context;
            if (!filters.MatchesContext(context))
            {
                continue;
            }

            var result = await invoker.Invoke(occurrence.Event, context);

            // A throwing reactor handler is a documented, load-bearing behavior in production - it pauses the
            // failing event-source partition so Chronicle can retry it. ReactorInvoker itself catches the
            // exception so it can report it to the kernel, which would otherwise make it indistinguishable from
            // "the reactor ran and did nothing" here - rethrow so Catch.Exception around Events(...) sees it,
            // matching what a spec author asserting the reactor's guard clearly expects (#3933).
            if (result.ExceptionResult.TryGetException(out var reactorException))
            {
                var handlerException = reactorException is TargetInvocationException { InnerException: { } inner }
                    ? inner
                    : reactorException;
                ExceptionDispatchInfo.Capture(handlerException).Throw();
            }
        }
    }

    void AssertProduced<TEvent>(Func<ProducedEvent, TEvent, bool> predicate, string expectation)
    {
        if (!ProducedEvents.Any(produced => produced.Event is TEvent @event && predicate(produced, @event)))
        {
            var destinations = ProducedEvents.Count > 0
                ? string.Join(", ", ProducedEvents.Select(produced =>
                    $"{produced.Event.GetType().Name} [source id '{produced.EventSourceId.Value}', source type '{produced.EventSourceType.Value}', stream type '{produced.EventStreamType.Value}', stream id '{produced.EventStreamId.Value}']"))
                : "nothing";
            throw new ReactorSideEffectAssertionException(
                $"Expected reactor '{typeof(TReactor).Name}' to produce a '{typeof(TEvent).Name}' {expectation}, but recorded destinations were: {destinations}.");
        }
    }

    IServiceProvider EnsureServiceProvider() =>
        _serviceProvider ??= _explicitServiceProvider ?? Services.BuildServiceProvider();

    IEventStore EnsureEventStore(IServiceProvider serviceProvider)
    {
        if (_eventStore is not null)
        {
            return _eventStore;
        }

        if (_explicitEventStore is not null)
        {
            if (_readModelSeeds.Count > 0 && _explicitEventStore is not EventStoreForTesting)
            {
                throw new CannotSeedReadModelWithExplicitEventStore(typeof(TReactor));
            }

            _eventStore = _explicitEventStore;
        }
        else
        {
            _eventStore = new EventStoreForTesting(serviceProvider, _defaults.ClientArtifactsProvider, _defaults.ScenarioOptions);
        }

        if (_eventStore is EventStoreForTesting inProcessEventStore)
        {
            foreach (var seed in _readModelSeeds)
            {
                seed(inProcessEventStore);
            }
        }

        return _eventStore;
    }
}
