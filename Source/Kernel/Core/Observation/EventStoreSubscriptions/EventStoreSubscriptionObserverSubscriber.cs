// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreSubscriptionObserverSubscriber"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for accessing event sequence grains.</param>
/// <param name="encryptionKeyStorage"><see cref="IEncryptionKeyStorage"/> for key propagation between event stores.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serializing event content.</param>
/// <param name="logger">The logger.</param>
public class EventStoreSubscriptionObserverSubscriber(
    IGrainFactory grainFactory,
    IEncryptionKeyStorage encryptionKeyStorage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<EventStoreSubscriptionObserverSubscriber> logger) : Grain, IEventStoreSubscriptionObserverSubscriber
{
    ObserverKey _key = ObserverKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (context.Metadata is not string targetEventStoreValue || string.IsNullOrEmpty(targetEventStoreValue))
        {
            logger.MissingTargetEventStore(_key);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ["Missing target event store in subscriber context"],
                string.Empty);
        }

        var targetEventStore = new EventStoreName(targetEventStoreValue);
        var inboxSequenceId = new EventSequenceId($"{EventSequenceId.InboxPrefix}{_key.EventStore}");
        var inboxSequence = grainFactory.GetEventSequence(inboxSequenceId, targetEventStore, _key.Namespace);

        try
        {
            var copiedSubjects = new HashSet<Subject>();
            foreach (var @event in events)
            {
                if (copiedSubjects.Add(@event.Context.Subject))
                {
                    await CopyEncryptionKeyIfMissingForTargetStore(@event.Context.Subject, targetEventStore);
                }
                var content = SerializeContent(@event.Content);
                await inboxSequence.Append(
                    @event.Context.EventSourceType,
                    @event.Context.EventSourceId,
                    @event.Context.EventStreamType,
                    @event.Context.EventStreamId,
                    @event.Context.EventType,
                    content,
                    @event.Context.CorrelationId,
                    @event.Context.Causation,
                    @event.Context.CausedBy,
                    [],
                    ConcurrencyScope.None,
                    subject: @event.Context.Subject);
            }

            logger.SuccessfullyForwardedEvents(_key, targetEventStore, inboxSequenceId);
            return ObserverSubscriberResult.Ok(events.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
        }
        catch (Exception ex)
        {
            logger.ErrorForwardingEvents(ex, _key, targetEventStore, inboxSequenceId);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                [ex.Message],
                ex.StackTrace ?? string.Empty);
        }
    }

    JsonObject SerializeContent(System.Dynamic.ExpandoObject content)
    {
        var json = JsonSerializer.Serialize(content, jsonSerializerOptions);
        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }

    async Task CopyEncryptionKeyIfMissingForTargetStore(Subject subject, EventStoreName targetEventStore)
    {
        var identifier = new EncryptionKeyIdentifier(subject.Value);
        var targetHasKey = await encryptionKeyStorage.HasFor(targetEventStore, _key.Namespace, identifier);
        if (targetHasKey)
        {
            return;
        }

        // The target may have erased this subject. Copying the source key in is precisely how an erased key came
        // back before the fence existed - the copy happens because the target holds no key, which is the state an
        // erasure creates. The store refuses the write regardless; asking first is what turns a thrown exception
        // into a logged refusal for the common case, so forwarding of the subject's non-PII events keeps flowing.
        if (await encryptionKeyStorage.GetErasureFor(targetEventStore, _key.Namespace, identifier) is { NewKeyAllowed: false })
        {
            logger.SkippedCopyingEncryptionKeyToErasedEventStore(_key.EventStore, targetEventStore, _key.Namespace);
            return;
        }

        var sourceKey = await encryptionKeyStorage.TryGetFor(_key.EventStore, _key.Namespace, identifier);
        if (sourceKey is null)
        {
            return;
        }

        // Idempotently place the source key in the target store. GetOrAddFor keeps any key the target already
        // has and otherwise persists the source key as the initial revision, so concurrent forwarded events for
        // the same subject — or a replay — converge on a single revision instead of minting duplicate revisions.
        await encryptionKeyStorage.GetOrAddFor(targetEventStore, _key.Namespace, identifier, sourceKey);

        // The propagation is otherwise invisible: nothing in the API tells an operator that a second event store
        // now holds this subject's key, and right-to-erasure reaches exactly the one event store it is asked for.
        // The identifier is deliberately not written. It is the compliance subject, which defaults to the event
        // source id and is a natural person in the common case, and a log line naming it is unencrypted personal
        // data that outlives the crypto-shred this whole feature exists to perform. The pair of event stores and
        // the namespace are what an operator needs to know a copy happened and where to look.
        logger.CopiedEncryptionKeyToTargetEventStore(_key.EventStore, targetEventStore, _key.Namespace);
    }
}
