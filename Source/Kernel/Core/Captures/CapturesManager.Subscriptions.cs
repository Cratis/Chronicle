// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures;

public partial class CapturesManager
{
    readonly Dictionary<CaptureId, CaptureLifecycle> _captureLifecycles = new();

    /// <inheritdoc/>
    public Task EnsureSubscribedIn(EventStoreNamespaceName @namespace)
    {
        if (_namespaceSubscriptions.TryGetValue(@namespace, out var pending))
        {
            return pending;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _namespaceSubscriptions.Add(@namespace, completion.Task);
        _ = CompleteNamespaceSubscriptions(@namespace, completion);
        return completion.Task;
    }

    async Task CompleteNamespaceSubscriptions(EventStoreNamespaceName @namespace, TaskCompletionSource completion)
    {
        try
        {
            await SubscribeInNamespace(@namespace);
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            _namespaceSubscriptions.Remove(@namespace);
        }
    }

    async Task SubscribeInNamespace(EventStoreNamespaceName @namespace)
    {
        foreach (var capture in (await Captures.GetAll()).Where(capture => capture.Status == CaptureStatus.Started))
        {
            try
            {
                await RunStartedCapture(capture.Id, @namespace);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.FailedSubscribingCaptureInNamespace(exception, capture.Name, capture.Id, @namespace);
            }
        }
    }

    async Task SubscribeCaptureInNamespace(EventStoreNamespaceName @namespace, CaptureDefinition definition)
    {
        var sequence = definition.Source.Sequence;
        if (string.IsNullOrWhiteSpace(sequence))
        {
            sequence = (await EventsCaptureSequence.ResolveFor(storage.GetEventStore(_eventStoreName).EventTypes, definition.Source)).Sequence;
        }

        if (string.IsNullOrWhiteSpace(sequence))
        {
            // Let the subscription path report its source-resolution diagnostics.
            await eventsSubscriptions.Recover(_eventStoreName, @namespace, definition);
            logger.SubscriptionNotEstablished(definition.Name, definition.Id, @namespace);
            return;
        }

        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(CaptureObservers.For(definition.Id), _eventStoreName, @namespace, new EventSequenceId(sequence)));
        if (await observer.IsSubscribed()) return;
        var state = await observer.GetState();
        if (state.AlertDisposition == AlertDisposition.Retired || state.RunningState == ObserverRunningState.Quarantined) return;

        // The caller holds this capture's lifecycle boundary across resolution and subscription.
        // Recovery itself checks observer eligibility inside its serialized request.
        await eventsSubscriptions.Recover(_eventStoreName, @namespace, definition);
        if (!await observer.IsSubscribed())
        {
            state = await observer.GetState();
            if (state.AlertDisposition != AlertDisposition.Retired && state.RunningState != ObserverRunningState.Quarantined)
            {
                logger.SubscriptionNotEstablished(definition.Name, definition.Id, @namespace);
            }
        }
    }

    async Task RunStartedCapture(CaptureId identifier, EventStoreNamespaceName? @namespace = null)
    {
        var lifecycle = GetLifecycle(identifier);
        if (lifecycle.Closed) return;
        await lifecycle.Gate.WaitAsync();
        try
        {
            if (lifecycle.Closed || !await Captures.Has(identifier)) return;
            var capture = await Captures.Get(identifier);
            if (lifecycle.Closed || capture.Status != CaptureStatus.Started) return;
            var definition = GetEventsDefinition(capture);
            if (definition is not null)
            {
                // Hold admission from fresh eligibility through all source resolution, namespace enumeration,
                // and observer dispatch. Stop/Delete close admission before awaiting and drain this entire unit.
                if (@namespace is not null)
                {
                    await SubscribeCaptureInNamespace(@namespace, definition);
                }
                else
                {
                    await eventsSubscriptions.Recover(_eventStoreName, definition);
                }
            }
            else if (@namespace is null)
            {
                await Run(capture);
            }
        }
        finally
        {
            lifecycle.Gate.Release();
        }
    }

    CaptureLifecycle GetLifecycle(CaptureId identifier)
    {
        if (!_captureLifecycles.TryGetValue(identifier, out var lifecycle))
        {
            _captureLifecycles.Add(identifier, lifecycle = new());
        }

        return lifecycle;
    }

    sealed class CaptureLifecycle
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public bool Closed { get; set; }
        public int Closure { get; private set; }

        public void Close()
        {
            Closed = true;
            Closure++;
        }
    }
}
