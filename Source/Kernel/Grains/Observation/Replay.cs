// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IReplay"/>.
/// </summary>
public class Replay : ObserverWorker, IReplay
{
    readonly ILogger<Replay> _logger;
    readonly List<FailedPartition> _failedPartitions = new();
    ObserverKey? _observerKey;
    IDisposable? _timer;
    bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="Replay"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Replay(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        [PersistentState(nameof(ObserverState), ObserverState.ReplayStorageProvider)] IPersistentState<ObserverState> observerState,
        ILogger<Replay> logger) : base(executionContextManager, eventSequenceStorageProvider, observerState, logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override MicroserviceId MicroserviceId => _observerKey!.MicroserviceId;

    /// <inheritdoc/>
    protected override TenantId TenantId => _observerKey!.TenantId;

    /// <inheritdoc/>
    protected override EventSequenceId EventSequenceId => _observerKey!.EventSequenceId;

    /// <inheritdoc/>
    protected override MicroserviceId? SourceMicroserviceId => _observerKey!.SourceMicroserviceId;

    /// <inheritdoc/>
    protected override TenantId? SourceTenantId => _observerKey!.SourceTenantId;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverSubscription subscription)
    {
        if (_isRunning)
        {
            _logger.AlreadyReplaying(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
            return;
        }

        await ReadStateAsync();

        _logger.Starting(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
        CurrentSubscription = subscription;
        _isRunning = true;
        _timer = RegisterTimer(PerformReplay, null, TimeSpan.Zero, TimeSpan.MaxValue);
    }

    /// <inheritdoc/>
    public async Task Stop()
    {
        _logger.Stopping(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
        _isRunning = false;
        _timer?.Dispose();
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public override Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        _failedPartitions.Add(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace));
        return Task.CompletedTask;
    }

    async Task PerformReplay(object arg)
    {
        _timer?.Dispose();
        try
        {
            var provider = EventSequenceStorageProvider;

            var next = State.NextEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : State.NextEventSequenceNumber;
            var nextSequenceNumber = await provider.GetNextSequenceNumberGreaterOrEqualThan(_observerKey!.EventSequenceId!, next, State.EventTypes);
            if (nextSequenceNumber == EventSequenceNumber.Unavailable)
            {
                nextSequenceNumber = EventSequenceNumber.First;
            }

            var tailSequenceNumber = await provider.GetTailSequenceNumber(_observerKey!.EventSequenceId!, State.EventTypes);
            var headSequenceNumber = await EventSequenceStorageProvider.GetHeadSequenceNumber(State.EventSequenceId, State.EventTypes);
            using var cursor = await provider.GetFromSequenceNumber(_observerKey!.EventSequenceId!, nextSequenceNumber, eventTypes: State.EventTypes);
            while (await cursor.MoveNext())
            {
                if (!_isRunning) break;

                foreach (var @event in cursor.Current)
                {
                    var state = EventObservationState.Replay;

                    if (@event.Metadata.SequenceNumber == headSequenceNumber)
                    {
                        state |= EventObservationState.HeadOfReplay;
                        _logger.HeadOfReplay(headSequenceNumber, ObserverId, _observerKey!.EventSequenceId);
                    }

                    if (@event.Metadata.SequenceNumber == tailSequenceNumber)
                    {
                        state |= EventObservationState.TailOfReplay;
                    }

                    var actualEvent = new AppendedEvent(@event.Metadata, @event.Context.WithState(state), @event.Content);

                    if (!_isRunning) break;
                    await Handle(actualEvent);
                }
            }

            _isRunning = false;
            _logger.Replayed(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
            await Supervisor.NotifyCatchUpComplete(_failedPartitions.ToArray());
            _failedPartitions.Clear();
        }
        catch (Exception ex)
        {
            _logger.ErrorDuringReplay(ObserverId, _observerKey!.EventSequenceId, ex);
        }
    }
}
