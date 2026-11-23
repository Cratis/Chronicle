// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
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
/// <param name="storage">Storage for current subscriptions and exact event generations.</param>
/// <param name="generationRelease">The pinned generation release boundary.</param>
/// <param name="namespaceCasing">The namespace casing resolver.</param>
public class EventStoreSubscriptionObserverSubscriber(
    IGrainFactory grainFactory,
    IEncryptionKeyStorage encryptionKeyStorage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<EventStoreSubscriptionObserverSubscriber> logger,
    IStorage storage,
    IEventGenerationRelease generationRelease,
    INamespaceCasing namespaceCasing) : Grain, IEventStoreSubscriptionObserverSubscriber
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
        var targetNamespace = await namespaceCasing.Canonicalize(targetEventStore, _key.Namespace);
        var inboxSequenceId = new EventSequenceId($"{EventSequenceId.InboxPrefix}{_key.EventStore}");
        var inboxSequence = grainFactory.GetEventSequence(inboxSequenceId, targetEventStore, targetNamespace);

        var generation = new EventStoreSubscriptionTargetGeneration(storage.GetEventStore(_key.EventStore).EventTypes, storage.GetEventStore(targetEventStore).EventTypes, generationRelease, _key.EventStore, targetEventStore, storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).GetEventSequence(_key.EventSequenceId));
        var state = new ForwardingState(inboxSequence, targetEventStore, targetNamespace, generation);
        try
        {
            var definition = await ResolveDefinition(targetEventStore);
            state.Pins = BuildPins(definition);
            foreach (var incoming in events)
            {
                if (!IsForSubscription(definition, incoming))
                {
                    // Never copied into the target inbox, but processed so the observer moves past it.
                    state.LastForwarded = incoming.Context.SequenceNumber;
                    continue;
                }

                var (@event, appendResult) = await ForwardEvent(incoming, state);
                if (!appendResult.IsSuccess)
                {
                    return FailAppend(state, @event, appendResult, inboxSequenceId);
                }

                state.LastForwarded = @event.Context.SequenceNumber;
            }

            logger.SuccessfullyForwardedEvents(_key, targetEventStore, inboxSequenceId);
            return ObserverSubscriberResult.Ok(state.LastForwarded);
        }
        catch (EventStoreSubscriptionGenerationUnavailable ex)
        {
            logger.ErrorForwardingEvents(ex, _key, targetEventStore, inboxSequenceId);
            return ObserverSubscriberResult.Failed(state.LastForwarded, ex.Message) with { FailurePolicy = WebhookDeliveryFailurePolicy.ParkPartition };
        }
        catch (Exception ex)
        {
            logger.ErrorForwardingEvents(ex, _key, targetEventStore, inboxSequenceId);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                state.LastForwarded,
                [ex.Message],
                ex.StackTrace ?? string.Empty);
        }
    }

    static Dictionary<EventTypeId, EventType> BuildPins(EventStoreSubscriptionDefinition? definition) =>
        definition?.GenerationDelivery == EventGenerationDelivery.Pinned
            ? definition.EventTypes.Distinct().ToDictionary(_ => _.Id)
            : [];

    static string DescribeFailureKind(AppendResult appendResult) => appendResult switch
    {
        { HasConstraintViolations: true } => "constraint violation",
        { HasConcurrencyViolations: true } => "concurrency violation",
        { HasErrors: true } => "append error",
        _ => "unknown append failure"
    };

    static Task<AppendResult> AppendToInbox(IEventSequence inboxSequence, AppendedEvent @event, JsonObject content)
    {
        var namedTags = @event.Context.NamedTags.ToArray();
        return namedTags.Length == 0
            ? inboxSequence.Append(
                @event.Context.EventSourceType,
                @event.Context.EventSourceId,
                @event.Context.EventStreamType,
                @event.Context.EventStreamId,
                @event.Context.EventType,
                content,
                @event.Context.CorrelationId,
                @event.Context.Causation,
                @event.Context.CausedBy,
                @event.Context.Tags,
                ConcurrencyScope.None,
                occurred: @event.Context.Occurred,
                subject: @event.Context.Subject)
            : inboxSequence.Append(
                @event.Context.EventSourceType,
                @event.Context.EventSourceId,
                @event.Context.EventStreamType,
                @event.Context.EventStreamId,
                @event.Context.EventType,
                content,
                @event.Context.CorrelationId,
                @event.Context.Causation,
                @event.Context.CausedBy,
                @event.Context.Tags,
                ConcurrencyScope.None,
                @event.Context.Occurred,
                @event.Context.Subject,
                namedTags);
    }

    static string DescribeViolations(AppendResult appendResult)
    {
        const int maxViolations = 5;
        var violations = appendResult.ConstraintViolations.ToArray();
        if (violations.Length == 0)
        {
            return string.Empty;
        }

        // Only schema violations carry their message: it holds the path and kind, never values. Other constraint
        // messages (such as unique) may contain the offending values and stay out of the failure text.
        var described = violations.Take(maxViolations).Select(_ => _.ConstraintType == ConstraintType.Schema
            ? $"{_.ConstraintType} '{_.ConstraintName.Value}' ({_.Message.Value})"
            : $"{_.ConstraintType} '{_.ConstraintName.Value}'");
        var more = violations.Length > maxViolations ? $" and {violations.Length - maxViolations} more" : string.Empty;
        return $": {string.Join(", ", described)}{more}";
    }

    async Task<EventStoreSubscriptionDefinition?> ResolveDefinition(EventStoreName targetEventStore)
    {
        // Read committed policy, not the manager grain: replay/reconciliation can await this delivery.
        // Resolve on every batch so an old Compatibility job also observes a deliberate mode update.
        var definitions = (await storage.GetEventStore(targetEventStore).EventStoreSubscriptions.GetAll())
            .Where(_ => _.SourceEventStore == _key.EventStore).ToArray();
        var definition = definitions.SingleOrDefault(_ => $"{_.Identifier.Value}->{targetEventStore.Value}" == _key.ObserverId.Value)
            ?? definitions.SingleOrDefault(_ => _.Identifier.Value == _key.ObserverId.Value);
        if (definition?.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            EventStoreSubscriptionGenerationValidation.ValidatePins(definition.EventTypes);
        }

        return definition;
    }

    bool IsForSubscription(EventStoreSubscriptionDefinition? definition, AppendedEvent incoming)
    {
        // Not for this subscription: no committed definition (fail closed - a removed subscription whose observer could not
        // be revoked must not keep forwarding unfiltered), a malformed source namespace (defense in depth behind the
        // manager revoking the observer) or a tag filter mismatch.
        if (definition is null)
        {
            logger.NoDefinitionForObserver(_key);
            return false;
        }

        return SubscriptionObservation.Observes(_key.Namespace)
            && EventStoreSubscriptionTagFilter.Matches(definition.NamedTags, incoming.Context.NamedTags);
    }

    async Task<(AppendedEvent Event, AppendResult Result)> ForwardEvent(AppendedEvent incoming, ForwardingState state)
    {
        var @event = await state.Generation.Release(incoming, state.Pins);
        if (state.CopiedSubjects.Add(@event.Context.Subject))
        {
            await CopyEncryptionKeyIfMissingForTargetStore(@event.Context.Subject, state.TargetEventStore, state.TargetNamespace);
        }

        var content = await MapToTargetSchema(SerializeContent(@event.Content), @event.Context.EventType, state.TargetSchemas, state.TargetEventStore);
        return (@event, await AppendToInbox(state.InboxSequence, @event, content));
    }

    ObserverSubscriberResult FailAppend(ForwardingState state, AppendedEvent @event, AppendResult appendResult, EventSequenceId inboxSequenceId)
    {
        var failureKind = DescribeFailureKind(appendResult);
        logger.FailedForwardingEvent(_key, state.TargetEventStore, inboxSequenceId, @event.Context.EventType.Id, failureKind);
        return ObserverSubscriberResult.Failed(
            state.LastForwarded,
            $"Inbox append {failureKind} for event type '{@event.Context.EventType.Id}'{DescribeViolations(appendResult)}");
    }

    async Task<JsonObject> MapToTargetSchema(
        JsonObject content,
        EventType eventType,
        Dictionary<EventType, JsonSchema?> targetSchemas,
        EventStoreName targetEventStore)
    {
        if (!targetSchemas.TryGetValue(eventType, out var schema))
        {
            var eventTypes = storage.GetEventStore(targetEventStore).EventTypes;
            schema = await eventTypes.HasFor(eventType.Id, eventType.Generation)
                ? (await eventTypes.GetFor(eventType.Id, eventType.Generation)).Schema
                : null;
            targetSchemas[eventType] = schema;
        }

        return schema is null ? content : EventStoreSubscriptionContentCasing.Map(content, schema);
    }

    JsonObject SerializeContent(System.Dynamic.ExpandoObject content)
    {
        var json = JsonSerializer.Serialize(content, jsonSerializerOptions);
        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }

    async Task CopyEncryptionKeyIfMissingForTargetStore(Subject subject, EventStoreName targetEventStore, EventStoreNamespaceName targetNamespace)
    {
        var identifier = new EncryptionKeyIdentifier(subject.Value);
        var targetHasKey = await encryptionKeyStorage.HasFor(targetEventStore, targetNamespace, identifier);
        if (targetHasKey)
        {
            return;
        }

        // The target may have erased this subject. Copying the source key in is precisely how an erased key came
        // back before the fence existed - the copy happens because the target holds no key, which is the state an
        // erasure creates. The store refuses the write regardless; asking first is what turns a thrown exception
        // into a logged refusal for the common case, so forwarding of the subject's non-PII events keeps flowing.
        if (await ReleaseKeys.GetErasureForRelease(encryptionKeyStorage, targetEventStore, targetNamespace, identifier) is { NewKeyAllowed: false })
        {
            logger.SkippedCopyingEncryptionKeyToErasedEventStore(_key.EventStore, targetEventStore, targetNamespace);
            return;
        }

        var sourceKey = await ReleaseKeys.TryGetForRelease(encryptionKeyStorage, _key.EventStore, _key.Namespace, identifier);
        if (sourceKey is null)
        {
            return;
        }

        // Idempotently place the source key in the target store. GetOrAddFor keeps any key the target already
        // has and otherwise persists the source key as the initial revision, so concurrent forwarded events for
        // the same subject — or a replay — converge on a single revision instead of minting duplicate revisions.
        await encryptionKeyStorage.GetOrAddFor(targetEventStore, targetNamespace, identifier, sourceKey);

        // The propagation is otherwise invisible: nothing in the API tells an operator that a second event store
        // now holds this subject's key, and right-to-erasure reaches exactly the one event store it is asked for.
        // The identifier is deliberately not written. It is the compliance subject, which defaults to the event
        // source id and is a natural person in the common case, and a log line naming it is unencrypted personal
        // data that outlives the crypto-shred this whole feature exists to perform. The pair of event stores and
        // the namespace are what an operator needs to know a copy happened and where to look.
        logger.CopiedEncryptionKeyToTargetEventStore(_key.EventStore, targetEventStore, targetNamespace);
    }

    sealed class ForwardingState(IEventSequence inboxSequence, EventStoreName targetEventStore, EventStoreNamespaceName targetNamespace, EventStoreSubscriptionTargetGeneration generation)
    {
        public IEventSequence InboxSequence { get; } = inboxSequence;

        public EventStoreName TargetEventStore { get; } = targetEventStore;

        public EventStoreNamespaceName TargetNamespace { get; } = targetNamespace;

        public Dictionary<EventTypeId, EventType> Pins { get; set; } = [];

        public EventStoreSubscriptionTargetGeneration Generation { get; } = generation;

        public Dictionary<EventType, JsonSchema?> TargetSchemas { get; } = [];

        public HashSet<Subject> CopiedSubjects { get; } = [];

        public EventSequenceNumber LastForwarded { get; set; } = EventSequenceNumber.Unavailable;
    }
}
