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
                var definition = GetEventsDefinition(capture);
                if (definition is not null)
                {
                    await SubscribeCaptureInNamespace(@namespace, definition);
                }
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
            await eventsSubscriptions.Subscribe(_eventStoreName, @namespace, definition);
            logger.SubscriptionNotEstablished(definition.Name, definition.Id, @namespace);
            return;
        }

        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(CaptureObservers.For(definition.Id), _eventStoreName, @namespace, new EventSequenceId(sequence)));
        if (await observer.IsSubscribed()) return;
        if ((await observer.GetState()).AlertDisposition == AlertDisposition.Retired) return;

        await eventsSubscriptions.Subscribe(_eventStoreName, @namespace, definition);
        if (!await observer.IsSubscribed() && (await observer.GetState()).AlertDisposition != AlertDisposition.Retired)
        {
            logger.SubscriptionNotEstablished(definition.Name, definition.Id, @namespace);
        }
    }
}
