// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Observation;

[StorageProvider(ProviderName = RecoverFailedPartitionState.StorageProvider)]
public class RecoverFailedPartition : Grain<RecoverFailedPartitionState>, IRecoverFailedPartition
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProviderProvider;
    readonly ILogger<RecoverFailedPartition> _logger;
    PartitionedObserverKey? _key;
    ObserverKey? _observerKey;
    ObserverSubscriberKey? _subscriberKey;
    ObserverSubscription? _subscriberSubscription;
    IDisposable? _timer;
    bool _isRunning = false;
    
    /// <summary>
    /// Gets the <see cref="IEventSequenceStorageProvider"/> in the correct context.
    /// </summary>
    protected IEventSequenceStorageProvider EventSequenceStorageProvider
    {
        get
        {
            var tenantId = _key!.TenantId;
            var microserviceId = _key.MicroserviceId;

            // TODO: This is a temporary work-around till we fix #264 & #265
            _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
            return _eventSequenceStorageProviderProvider();
        }
    }
    
    public RecoverFailedPartition(
        IExecutionContextManager executionContextManager, 
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        ILogger<RecoverFailedPartition> logger
    )
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        State.ObserverId = this.GetPrimaryKey(out var keyAsString);
        _key = PartitionedObserverKey.Parse(keyAsString);
        State.Id = _key.ToString();
        _observerKey = string.IsNullOrWhiteSpace(State.ObserverKey) ? null : ObserverKey.Parse(State.ObserverKey);
        _subscriberKey = string.IsNullOrWhiteSpace(State.SubscriberKey) ? null : ObserverSubscriberKey.Parse(State.SubscriberKey);
        await SetSubscriberSubscription();
        ScheduleNextTimer();
    }

    async Task SetSubscriberSubscription()
    {
        if (_observerKey is not null && State.ObserverId != ObserverId.Unspecified)
        {
            _subscriberSubscription = await GetSupervisor(State.ObserverId, _observerKey).GetCurrentSubscription();
        }
    }

    /// <inheritdoc/>
    public async Task Recover(EventSequenceNumber fromEvent, IEnumerable<EventType> eventTypes, ObserverKey observerKey)
    {
        _observerKey = observerKey;
        _subscriberKey = ObserverSubscriberKey.FromObserverKey(observerKey, _key!.EventSourceId);
        State.InitialiseError(fromEvent, eventTypes, _observerKey, _subscriberKey);
        await SetSubscriberSubscription();
        await WriteStateAsync();
        ScheduleNextTimer();
    }
    
    /// <inheritdoc/>
    public Task Catchup(EventSequenceNumber fromEvent, IEnumerable<EventType> eventTypes) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task Reset()
    {
        State.Reset();
        _timer?.Dispose();
        await WriteStateAsync();
    }

    async Task PerformRecovery(object state)
    {
        var provider = EventSequenceStorageProvider;
        _timer?.Dispose(); // we don't want to run this again until we are done
        using var cursor = await provider.GetFromSequenceNumber(_key!.EventSequenceId!, State.NextSequenceNumberToProcess, _key.EventSourceId, eventTypes: State.EventTypes);
        var completed = true;
        
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (!await Handle(@event)) continue;
                completed = false;
                break;
            }
        }

        if (completed)
        {
            await GetSupervisor(State.ObserverId, _observerKey!).NotifyFailedPartitionRecoveryComplete(State.NextSequenceNumberToProcess-1); 
        }
        else
        {
            ScheduleNextTimer();
        }
    }

    void ScheduleNextTimer()
    {
        if(State.HasBeenInitialised())
            _timer = RegisterTimer(PerformRecovery, null, State.GetNextAttemptSchedule(), State.GetNextAttemptSchedule());
    }


    public async Task<bool> Handle(AppendedEvent @event)
    {
        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        try
        {
            var next = State.NextSequenceNumberToProcess;

            if (@event.Metadata.SequenceNumber < next)
            {
                return true;
            }

            if (_subscriberSubscription is not null)
            {
                var subscriber = (GrainFactory.GetGrain(_subscriberSubscription.SubscriberType, State.ObserverId, _subscriberKey!) as IObserverSubscriber)!;
                var result = await subscriber.OnNext(@event, new ObserverSubscriberContext(_subscriberSubscription.Arguments));
                switch (result.State)
                {
                    case ObserverSubscriberState.Failed:
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        break;
                    case ObserverSubscriberState.Disconnected:
                        failed = true;
                        exceptionMessages = new[] { "Subscriber disconnected" };
                        exceptionStackTrace = string.Empty;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            failed = true;
            exceptionMessages = ex.GetAllMessages().ToArray();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
        }

        if (failed)
        {
            State.UpdateWithLatestError(@event.Metadata.SequenceNumber, exceptionMessages, exceptionStackTrace, DateTimeOffset.UtcNow);
        }
        else
        {
            State.UpdateWithLatestSuccess(@event);
        }
        await WriteStateAsync();
        return failed;
    }

    IObserverSupervisor GetSupervisor(ObserverId observerId, ObserverKey key) => GrainFactory.GetGrain<IObserverSupervisor>(observerId, key);
}