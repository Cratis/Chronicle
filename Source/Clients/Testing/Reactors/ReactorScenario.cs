// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Testing.Events;
using Cratis.Execution;
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
/// materialized from read models seeded via <c>Given.ForEventSourceId(...).ReadModel(...)</c>.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new ReactorScenario&lt;MyReactor&gt;();
/// scenario.Services.AddSingleton(_someService);
/// scenario.Given.ForEventSourceId(myId).ReadModel(new MyReadModel(...));
/// await scenario.Given.ForEventSource(myId).Events(new SomeEvent(), new AnotherEvent());
/// // Assert on side-effects captured by the mocks registered in Services
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
public class ReactorScenario<TReactor>
    where TReactor : class, IReactor
{
    readonly IReactorSideEffectHandlers? _sideEffectHandlers;
    readonly IEventStore? _explicitEventStore;
    readonly IServiceProvider? _explicitServiceProvider;
    readonly IEventTypes _eventTypes = Defaults.Instance.EventTypes;
    readonly List<Action<EventStoreForTesting>> _readModelSeeds = [];
    readonly RecordingReactorSideEffectHandlers _recordingHandlers = new();
    IServiceProvider? _serviceProvider;
    IEventStore? _eventStore;

    /// <summary>
    /// The sequence number the next event handed to the reactor is given.
    /// </summary>
    /// <remarks>
    /// Every event the scenario delivers gets its own number, contiguously from the first, across every
    /// <c>Given</c> call. Without that, a reactor keyed on <see cref="ReactorDelivery"/> would see two distinct
    /// events as the same delivery and skip the second - the scenario would report an idempotent reactor broken.
    /// </remarks>
    EventSequenceNumber _nextSequenceNumber = EventSequenceNumber.First;

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
    {
        _explicitServiceProvider = serviceProvider;
        _sideEffectHandlers = sideEffectHandlers;
        _explicitEventStore = eventStore;
        Services = new ServiceCollection();
        Services.AddLogging();
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
    /// <code>
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
    /// Seeds a pre-built read model instance for a specific event source so that a read-model handler-method parameter
    /// of the reactor is materialized with it.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to seed.</typeparam>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the read model instance with.</param>
    /// <param name="readModel">The read model instance to seed.</param>
    internal void SeedReadModel<TReadModel>(EventSourceId eventSourceId, TReadModel readModel)
        where TReadModel : class =>
        _readModelSeeds.Add(store => store.RegisterReadModelInstance(eventSourceId, readModel));

    /// <summary>
    /// Invokes the reactor with the provided events for a specific event source.
    /// </summary>
    /// <remarks>
    /// A fresh instance of <typeparamref name="TReactor"/> is activated from the service provider for each invocation,
    /// matching the production behavior where a new scope is created per event batch.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to invoke the reactor with, in order.</param>
    /// <returns>A <see cref="Task"/> that completes when all events have been handled.</returns>
    /// <exception cref="CannotActivateReactorForScenario">Thrown when the reactor's dependencies cannot be resolved.</exception>
    internal async Task InvokeWith(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        var serviceProvider = EnsureServiceProvider();
        var eventStore = EnsureEventStore(serviceProvider);

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
            argumentsResolver: null,
            serviceProvider: serviceProvider);

        foreach (var @event in events)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            var context = EventContext.From(
                "testing",
                "default",
                eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                _nextSequenceNumber,
                CorrelationId.New());

            _nextSequenceNumber += 1;
            await invoker.Invoke(@event, context);
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
            _eventStore = new EventStoreForTesting(serviceProvider);
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
