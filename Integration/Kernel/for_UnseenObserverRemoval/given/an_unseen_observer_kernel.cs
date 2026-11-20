// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// An in-process kernel with real storage and an event store of its own, where clients can be started and then made to go away,
/// and where the removal of observers no client has used can be swept on demand.
/// </summary>
/// <typeparam name="TFixture">The type of the fixture, which decides how the kernel is configured.</typeparam>
/// <param name="fixture">The fixture that hosts the in-process kernel.</param>
public abstract class an_unseen_observer_kernel<TFixture>(TFixture fixture) : Specification<TFixture>(fixture)
    where TFixture : UnseenObserverFixture
{
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    readonly List<OwnedClient> _clients = [];

    protected string _storeName = string.Empty;
    protected IEventStore _store = default!;

    /// <inheritdoc/>
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => [typeof(UnseenEvent)];

    /// <inheritdoc/>
    public override IEnumerable<Type> Reactors => [];

    /// <inheritdoc/>
    public override IEnumerable<Type> Reducers => [];

    /// <summary>
    /// Gets how many observers the removal has counted so far.
    /// </summary>
    public long CountedRemovals => ChronicleFixture.Removed;

    /// <summary>
    /// Gets the name of the event store of the specification.
    /// </summary>
    public string StoreName => _storeName;

    protected static DateTimeOffset Now => DateTimeOffset.UtcNow;

    protected IStorage KernelStorage => Services.GetRequiredService<IStorage>();

    protected IEventStoreStorage StoreStorage => KernelStorage.GetEventStore(_storeName);

    protected UnseenObserverRemoval Sweeper => Services.GetServices<ILifecycleParticipant<ISiloLifecycle>>().OfType<UnseenObserverRemoval>().Single();

    /// <summary>
    /// Gets the messages the removal logged at a level.
    /// </summary>
    /// <param name="level">The <see cref="LogLevel"/>.</param>
    /// <returns>The messages.</returns>
    public string[] LoggedAt(LogLevel level) => ChronicleFixture.Logs.At(level);

    /// <inheritdoc/>
    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(ChronicleFixture.Settings).Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
            services.AddSingleton<ILoggerProvider>(ChronicleFixture.Logs);
            services.Configure<LoggerFilterOptions>(options => options.Rules.Add(new LoggerFilterRule(null, typeof(UnseenObserverRemoval).FullName, LogLevel.Information, null)));
        });
    };

    /// <summary>
    /// Starts a client that owns the given reactors and reducers, connected to the given namespace of the event store.
    /// </summary>
    /// <param name="namespace">The namespace to connect to.</param>
    /// <param name="observers">The reactor and reducer instances the client owns.</param>
    /// <returns>The <see cref="OwnedClient"/>.</returns>
    protected async Task<OwnedClient> StartClient(string @namespace, params object[] observers) => await StartClient(_storeName, @namespace, observers);

    /// <summary>
    /// Starts a client that owns the given reactors and reducers, connected to the default namespace of the event store.
    /// </summary>
    /// <param name="observers">The reactor and reducer instances the client owns.</param>
    /// <returns>The <see cref="OwnedClient"/>.</returns>
    protected async Task<OwnedClient> StartClient(params object[] observers) => await StartClient(_storeName, EventStoreNamespaceName.Default.Value, observers);

    /// <summary>
    /// Starts a client that owns the given reactors and reducers, connected to a namespace of an event store.
    /// </summary>
    /// <param name="eventStore">The event store to connect to.</param>
    /// <param name="namespace">The namespace to connect to.</param>
    /// <param name="observers">The reactor and reducer instances the client owns.</param>
    /// <returns>The <see cref="OwnedClient"/>.</returns>
    protected async Task<OwnedClient> StartClient(string eventStore, string @namespace, params object[] observers)
    {
        var client = await OwnedClient.Start(Services, ChronicleClient.Options.DefaultSinkTypeId, eventStore, @namespace, observers);
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// Makes a client go away and waits until the kernel has noticed: nothing subscribes the observer any more.
    /// </summary>
    /// <param name="client">The <see cref="OwnedClient"/> that goes away.</param>
    /// <param name="observerId">The id of an observer the client owned.</param>
    /// <param name="namespace">The namespace the client was connected to.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task GoAway(OwnedClient client, string observerId, string? @namespace = default)
    {
        _clients.Remove(client);
        await client.DisposeAsync();
        await WaitUntil($"the kernel to notice that the client of {observerId} is gone", () => IsUnsubscribed(observerId, @namespace));
    }

    /// <summary>
    /// Makes a client go away and waits until nothing subscribes the observer, whatever state the observer was left in.
    /// </summary>
    /// <param name="client">The <see cref="OwnedClient"/> that goes away.</param>
    /// <param name="observerId">The id of an observer the client owned.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task GoAwayLeavingTheObserverAsItIs(OwnedClient client, string observerId)
    {
        _clients.Remove(client);
        await client.DisposeAsync();
        var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(Key(observerId));
        await WaitUntil($"the kernel to notice that the client of {observerId} is gone", async () => !await observer.IsSubscribed());
    }

    protected async Task<bool> IsUnsubscribed(string observerId, string? @namespace = default)
    {
        var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(Key(observerId, @namespace));
        return !await observer.IsSubscribed() && (await States(@namespace).Get(observerId)).RunningState == ObserverRunningState.Disconnected;
    }

    protected ObserverKey Key(string observerId, string? @namespace = default) =>
        new(observerId, _storeName, @namespace ?? EventStoreNamespaceName.Default.Value, EventSequenceId.Log);

    protected IObserverStateStorage States(string? @namespace = default) =>
        StoreStorage.GetNamespace(@namespace ?? EventStoreNamespaceName.Default.Value).Observers;

    /// <summary>
    /// Gets when a client was last seen holding the subscription of an observer, as the kernel recorded it.
    /// </summary>
    /// <param name="observerId">The id of the observer.</param>
    /// <param name="namespace">The namespace of the observer's state.</param>
    /// <returns>The time recorded.</returns>
    /// <remarks>
    /// The storage starts the clock of an observer that has none at the time it is asked, so this is for observers that are
    /// known to have one.
    /// </remarks>
    protected async Task<DateTimeOffset> LastSeen(string observerId, string? @namespace = default) =>
        (await States(@namespace).GetLastSeenAt([observerId], DateTimeOffset.UnixEpoch))[observerId];

    /// <summary>
    /// Reads an instance of the reducer's read model straight from the kernel, which is what a client asking for it gets.
    /// </summary>
    /// <param name="key">The key of the instance.</param>
    /// <returns>The read model, or null when there is none.</returns>
    protected async Task<UnseenReadModel?> ReadModel(string key)
    {
        var response = await ((IChronicleServicesAccessor)_store.Connection).Services.ReadModels.GetInstanceByKey(new GetInstanceByKeyRequest
        {
            EventStore = _storeName,
            Namespace = EventStoreNamespaceName.Default.Value,
            ReadModelIdentifier = typeof(UnseenReadModel).GetReadModelIdentifier(),
            EventSequenceId = EventSequenceId.Log.Value,
            ReadModelKey = key
        });
        return JsonSerializer.Deserialize<UnseenReadModel>(response.ReadModel, _json);
    }

    /// <summary>
    /// Makes an observer look as if no client had held it for a long time.
    /// </summary>
    /// <param name="observerId">The id of the observer.</param>
    /// <param name="age">How long ago it was last seen.</param>
    /// <param name="namespace">The namespace of the observer's state.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Backdate(string observerId, TimeSpan age, string? @namespace = default) =>
        (await States(@namespace).SaveLastSeenAt(observerId, Now - age, EventSequenceId.Log)).ShouldBeTrue();

    /// <summary>
    /// Waits until an observer has been unseen for longer than the period.
    /// </summary>
    /// <param name="observerId">The id of the observer.</param>
    /// <param name="namespace">The namespace of the observer's state.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task WaitUntilUnseenForThePeriod(string observerId, string? @namespace = default)
    {
        var seen = await LastSeen(observerId, @namespace);
        await WaitUntil($"{observerId} to be unseen for {UnseenObserverFixture.Period}", () => Task.FromResult(Now > seen + UnseenObserverFixture.Period + TimeSpan.FromMilliseconds(250)));
    }

    /// <summary>
    /// Describes everything the kernel holds for an observer in a namespace.
    /// </summary>
    /// <param name="observerId">The id of the observer.</param>
    /// <param name="namespace">The namespace of the observer's state.</param>
    /// <returns>The <see cref="Footprint"/>.</returns>
    protected async Task<Footprint> FootprintOf(string observerId, string? @namespace = default)
    {
        var name = @namespace ?? EventStoreNamespaceName.Default.Value;
        var grains = Services.GetRequiredService<IGrainFactory>();
        var jobs = await grains.GetJobsManager(_storeName, name).GetAllJobs();
        var reminders = await Services.GetRequiredService<IReminderTable>().ReadRows(grains.GetGrain<IObserver>(Key(observerId, name)).GetGrainId());
        var namespaceStorage = StoreStorage.GetNamespace(name);

        return new Footprint(
            await StoreStorage.Observers.Has(observerId),
            (await States(name).Get(observerId)).Identifier == observerId,
            (await namespaceStorage.FailedPartitions.GetFor(observerId)).Partitions.Count(),
            jobs.Count(job => job.Request is IObserverJobRequest request && request.ObserverKey.ObserverId == observerId),
            (await States(name).GetDurableSubscriptions(EventSequenceId.Log)).ContainsKey(observerId),
            reminders.Reminders.Count,
            (await _store.Observers.GetAll()).Any(observer => observer.Id == observerId));
    }

    /// <summary>
    /// Waits until the observer of a reactor or reducer has handled the events, in the order they were appended.
    /// </summary>
    /// <param name="observer">The reactor or reducer.</param>
    /// <param name="appended">The sequence numbers appended.</param>
    /// <returns>Awaitable task.</returns>
    protected static async Task WaitForHandledInOrder(IUnseenObserver observer, EventSequenceNumber[] appended)
    {
        await WaitForHandled(observer, appended.Length);
        observer.Handled.ToArray().ShouldEqual(appended);
    }

    /// <summary>
    /// Appends events to the event store and returns the sequence numbers they were given.
    /// </summary>
    /// <param name="store">The event store to append to.</param>
    /// <param name="count">How many events to append.</param>
    /// <param name="eventSourceId">The event source the events are for.</param>
    /// <returns>The sequence numbers, in order.</returns>
    protected static async Task<EventSequenceNumber[]> Append(IEventStore store, int count, string eventSourceId = "source")
    {
        var appended = new List<EventSequenceNumber>();
        for (var i = 0; i < count; i++)
        {
            appended.Add((await store.EventLog.Append(eventSourceId, new UnseenEvent($"event {i}"))).SequenceNumber);
        }

        return [.. appended];
    }

    /// <summary>
    /// Waits until a condition holds.
    /// </summary>
    /// <param name="what">What is waited for, for the failure message.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="seconds">How long to wait at the most.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the condition does not hold in time.</exception>
    protected static async Task WaitUntil(string what, Func<Task<bool>> condition, int seconds = 60)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
        while (!await condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {what}");
            }

            await Task.Delay(100);
        }
    }

    protected static async Task WaitForHandled(IUnseenObserver observer, int count) =>
        await WaitUntil($"{count} events to be handled, {observer.Handled.Count} were", () => Task.FromResult(observer.Handled.Count >= count));

    async Task Establish()
    {
        _storeName = $"unseen-{Guid.NewGuid():N}";
        _store = await ChronicleClient.GetEventStore(_storeName);
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
    }

    async Task Destroy()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
    }
}
