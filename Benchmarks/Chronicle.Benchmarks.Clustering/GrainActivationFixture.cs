// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.DependencyInjection;
using KernelEventSequenceKey = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey;
using KernelNamespaceName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using KernelObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using KernelObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverKey;
using KernelObserverOwner = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverOwner;
using KernelObserverRunningState = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverRunningState;
using KernelObserverType = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverType;
using KernelSequence = KernelCore::Cratis.Chronicle.EventSequences.IEventSequence;
using SequenceNumber = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Owns a fixed durable dataset and prepares the exact target without timing setup work.</summary>
/// <param name="mode">The requested lifecycle.</param>
/// <param name="sequence">Whether the target is the event sequence rather than observer.</param>
/// <param name="artifactDirectory">The absolute run artifact directory.</param>
public sealed class GrainActivationFixture(GrainActivationMode mode, bool sequence, string artifactDirectory) : IAsyncDisposable
{
    const ulong SeededEvents = 10;
    static readonly TimeSpan _callTimeout = TimeSpan.FromSeconds(30);
    readonly ActivationMongoHost _mongo = new();
    readonly ActivationEvidence _evidence = new(artifactDirectory, sequence ? "FirstEventSequenceRead" : "FirstObserverStateRead");
    readonly string _sequenceId = $"activation-{Guid.NewGuid():N}";
    readonly string _observerId = $"activation-observer-{Guid.NewGuid():N}";
    ClusterBenchmarkFixture? _cluster;
    ActivationIteration _iteration = null!;
    KernelSequence _sequence = null!;
    KernelObserver _observer = null!;
    IStorage _storage = null!;
    SequenceNumber _lastSequence = null!;
    ObserverState _lastState = null!;
    GrainId Target => sequence ? _sequence.GetGrainId() : _observer.GetGrainId();

    /// <summary>Starts the existing single-silo harness and verifies a non-empty persisted corpus.</summary>
    /// <returns>The setup task.</returns>
    public Task Start() => RunPhase("setup", async () =>
    {
        if (!Path.IsPathFullyQualified(artifactDirectory)) throw new ArgumentException("ACTIVATION_ARTIFACTS must be an absolute path.");
        _evidence.Begin(mode, default, Guid.Empty);
        await _mongo.Start();
        _cluster = new(
            ClusterTopology.SingleSilo,
            services =>
            {
                _evidence.ResetContexts();
                services.AddSingleton<IConfigureGrainContextProvider>(_evidence);
            },
            externalMongoUrl: _mongo.ConnectionString);
        await _cluster.Start().WaitAsync(TimeSpan.FromSeconds(90));
        Refresh();
        await Seed();
        _iteration = new(Prime, Deactivate, Restart, () => _evidence.Capture(_cluster.Generation));
    });

    /// <summary>Completes lifecycle work before the single measured call.</summary>
    /// <returns>The preparation task.</returns>
    public Task Prepare() => RunPhase("prepare", async () =>
    {
        _evidence.Begin(mode, Target, _cluster!.Generation);
        await _iteration.Prepare(mode);
    });

    /// <summary>Calls only the real event-sequence read, retaining the result for cleanup.</summary>
    /// <returns>The next sequence number.</returns>
    public async Task<SequenceNumber> ReadSequence() => _lastSequence = await _sequence.GetNextSequenceNumber().WaitAsync(_callTimeout);

    /// <summary>Calls only the real observer state read, retaining the result for cleanup.</summary>
    /// <returns>The loaded observer state.</returns>
    public async Task<ObserverState> ReadObserver() => _lastState = await _observer.GetState().WaitAsync(_callTimeout);

    /// <summary>Verifies durable markers and activation transitions outside timing.</summary>
    /// <returns>The verification task.</returns>
    public Task Verify() => RunPhase("verify", async () =>
    {
        await Validate().WaitAsync(_callTimeout);
        _iteration.Verify(() => _evidence.Verify(ResultDescription()));
    });

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_cluster is not null) await RunPhase("stop-host", () => _cluster.DisposeAsync().AsTask());
        }
        finally
        {
            await RunPhase("dispose-mongo", () => _mongo.DisposeAsync().AsTask());
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    void Refresh()
    {
        _storage = _cluster!.Services.GetRequiredService<IStorage>();
        var sequenceKey = new KernelEventSequenceKey(_sequenceId, ClusterBenchmarkFixture.EventStore, KernelNamespaceName.Default);
        _sequence = _cluster.GrainFactory.GetGrain<KernelSequence>(sequenceKey);
        var observerKey = new KernelObserverKey(_observerId, ClusterBenchmarkFixture.EventStore, KernelNamespaceName.Default, _sequenceId);
        _observer = _cluster.GrainFactory.GetGrain<KernelObserver>(observerKey);
    }

    async Task Seed()
    {
        var clientSequence = _cluster!.EventStore1.GetEventSequence(_sequenceId);
        for (ulong index = 0; index < SeededEvents; index++)
        {
            var result = await clientSequence.Append(EventSourceId.New(), new AppendOnlyEvent("activation", (int)index)).WaitAsync(_callTimeout);
            Require(result.IsSuccess, "Seed append failed.");
            Require(result.SequenceNumber.Value == index, "Seed corpus has the wrong sequence position.");
        }

        var definition = new ObserverDefinition(_observerId, [], _sequenceId, KernelObserverType.Unknown, KernelObserverOwner.None, false);
        await _storage.GetEventStore(ClusterBenchmarkFixture.EventStore).Observers.Save(definition).WaitAsync(_callTimeout);
        var state = new ObserverState
        {
            Identifier = _observerId,
            Owner = _sequenceId,
            HandledEventCount = SeededEvents,
            LastHandledEventSequenceNumber = SeededEvents - 1,
            NextEventSequenceNumber = SeededEvents,
            RunningState = KernelObserverRunningState.Disconnected
        };
        var saved = await NamespaceStorage().Observers.Save(state, _sequenceId).WaitAsync(_callTimeout);
        Require(saved, "Observer seed was not persisted.");
        await ReadSequence();
        await ReadObserver();
        await ValidateSequence().WaitAsync(_callTimeout);
        await ValidateObserver().WaitAsync(_callTimeout);
    }

    async Task Prime()
    {
        if (sequence) await ReadSequence();
        else await ReadObserver();
        await Validate().WaitAsync(_callTimeout);
        _evidence.RememberActivation();
    }

    async Task Deactivate()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _cluster!.GrainFactory.GetGrain<IManagementGrain>(0).DeactivateGrain(Target, deadline.Token).WaitAsync(deadline.Token);
    }

    async Task Restart()
    {
        await _cluster!.Restart();
        Refresh();
    }

    Task Validate() => sequence ? ValidateSequence() : ValidateObserver();

    async Task ValidateSequence()
    {
        Require(_lastSequence.Value == SeededEvents, "Sequence did not rehydrate the retained position.");
        var tail = await NamespaceStorage().GetEventSequence(_sequenceId).GetTailSequenceNumber();
        Require(tail.Value == SeededEvents - 1, "Seed corpus was not retained in MongoDB.");
    }

    async Task ValidateObserver()
    {
        ValidateState(_lastState);
        ValidateState(await NamespaceStorage().Observers.Get(_observerId));
        var definition = await _observer.GetDefinition();
        Require(definition.Identifier.Value == _observerId, "Loaded the wrong observer definition.");
        Require(definition.EventSequenceId.Value == _sequenceId, "Observer definition lost its sequence.");
        Require(!definition.EventTypes.Any(), "Seed observer must remain unsubscribed.");
    }

    void ValidateState(ObserverState state)
    {
        Require(state.Identifier.Value == _observerId, "Loaded the wrong observer state.");
        Require(state.HandledEventCount.Value == SeededEvents, "Observer lost its handled-event marker.");
        Require(state.LastHandledEventSequenceNumber.Value == SeededEvents - 1, "Observer lost its checkpoint.");
        Require(state.NextEventSequenceNumber.Value == SeededEvents, "Observer lost its next position.");
        Require(!state.IsReplaying, "Seed observer unexpectedly started replaying.");
        Require(!state.FailedPartitions.Any(), "Seed observer unexpectedly has failed partitions.");
    }

    object ResultDescription() => sequence
        ? new { SeededEvents, NextSequenceNumber = _lastSequence.Value }
        : new { SeededEvents, Observer = _lastState.Identifier.Value, HandledEvents = _lastState.HandledEventCount.Value,
            Checkpoint = _lastState.LastHandledEventSequenceNumber.Value, NextSequenceNumber = _lastState.NextEventSequenceNumber.Value };

    IEventStoreNamespaceStorage NamespaceStorage() => _storage.GetEventStore(ClusterBenchmarkFixture.EventStore).GetNamespace(KernelNamespaceName.Default);

    async Task RunPhase(string phase, Func<Task> action)
    {
        try { await action(); }
        catch (Exception error)
        {
            _evidence.Failure(phase, error);
            throw;
        }
    }
}
