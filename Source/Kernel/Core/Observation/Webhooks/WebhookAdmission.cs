// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;
using WebhookDefinition = Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Validates the delivery format of webhook registrations and admits <see cref="WebhookAdded"/> events atomically
/// against the lifecycle of the webhook.
/// </summary>
/// <remarks>
/// The delivery format is fixed when a webhook is created. For an envelope webhook that is not yet registered the
/// lifecycle position is read first, and only then is the observer history checked, so that admission appends
/// against the very position the checks were made at.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for reading the system event sequence and observers.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and reading back registration events.</param>
/// <param name="logger">The logger.</param>
class WebhookAdmission(
    IGrainFactory grainFactory,
    IStorage storage,
    IEventSerializer eventSerializer,
    ILogger<WebhookRegistrar> logger)
{
    static readonly EventType[] _lifecycleEventTypes = [typeof(WebhookAdded).GetEventType(), typeof(WebhookRemoved).GetEventType()];
    static readonly EventTypeId[] _lifecycleEventTypeIds = [.. _lifecycleEventTypes.Select(_ => _.Id)];

    /// <summary>
    /// Validates the delivery formats of a request, before anything is appended.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the webhooks belong to.</param>
    /// <param name="webhooks">The requested webhook definitions.</param>
    /// <returns>The <see cref="WebhookAdmissionContext"/> to admit the registrations with.</returns>
    /// <exception cref="ConflictingWebhookDeliveryFormatsInRequest">One webhook appears with different delivery formats in the request.</exception>
    /// <exception cref="WebhookDeliveryFormatCannotBeChanged">The delivery format of an existing webhook is changed.</exception>
    /// <exception cref="EnvelopeWebhookIdentifierHasObserverHistory">An envelope webhook is added under an identifier with observer history.</exception>
    /// <exception cref="WebhookLifecycleChangedDuringRegistration">The lifecycle of an envelope webhook is already registered but not yet applied.</exception>
    public async Task<WebhookAdmissionContext> Validate(EventStoreName eventStore, IEnumerable<ContractWebhookDefinition> webhooks)
    {
        var context = new WebhookAdmissionContext(eventStore);
        var requestedByIdentifier = webhooks.GroupBy(_ => _.Identifier).ToArray();
        foreach (var group in requestedByIdentifier)
        {
            var formats = group.Select(_ => _.ToChronicle().DeliveryFormat).Distinct().ToArray();
            if (formats.Length > 1)
            {
                throw new ConflictingWebhookDeliveryFormatsInRequest(group.Key, formats.Select(_ => _.ToString()));
            }
        }

        var existingWebhooks = (await grainFactory.GetGrain<Chronicle.Observation.Webhooks.IWebhooks>(eventStore).GetWebhookDefinitions()).ToArray();
        foreach (var group in requestedByIdentifier)
        {
            var requested = group.First().ToChronicle();
            var existing = existingWebhooks.FirstOrDefault(_ => _.Identifier == requested.Identifier);
            if (existing is not null)
            {
                EnsureSameFormat(existing, requested);
            }
            else if (requested.UsesDeliveryEnvelope())
            {
                context.Positions[group.Key] = await ReadLifecyclePositionAndCheckObserverHistory(eventStore, requested);
            }
        }

        return context;
    }

    /// <summary>
    /// Appends the <see cref="WebhookAdded"/> event for a new webhook, atomically against the webhook's lifecycle.
    /// </summary>
    /// <remarks>
    /// The system event sequence validates the concurrency scope and writes within one turn, so of two registrations
    /// made against the same lifecycle position only one lands. An envelope registration is made against the position
    /// its checks were made at and fails closed on any conflict. An identical legacy registration is idempotent.
    /// </remarks>
    /// <param name="eventSequence">The system <see cref="IEventSequence"/>.</param>
    /// <param name="webhook">The webhook being registered.</param>
    /// <param name="added">The event to append.</param>
    /// <param name="context">The <see cref="WebhookAdmissionContext"/> of the request.</param>
    /// <returns>True if the event was appended, false if an identical registration already exists.</returns>
    public Task<bool> Admit(IEventSequence eventSequence, WebhookDefinition webhook, WebhookAdded added, WebhookAdmissionContext context)
    {
        var id = webhook.Identifier.Value;
        if (!webhook.UsesDeliveryEnvelope())
        {
            return AdmitLegacy(eventSequence, id, added, context.EventStore);
        }

        // The same envelope webhook appeared earlier in this request and was admitted then.
        return context.Admitted.Add(id) ? AdmitEnvelope(eventSequence, id, added, context) : Task.FromResult(false);
    }

    static void EnsureSameFormat(WebhookDefinition existing, WebhookDefinition requested)
    {
        if (existing.DeliveryFormat != requested.DeliveryFormat)
        {
            throw new WebhookDeliveryFormatCannotBeChanged(requested.Identifier, existing.DeliveryFormat, requested.DeliveryFormat);
        }
    }

    static bool IsWebhookAdded(AppendedEvent appended) => appended.Context.EventType.Id == typeof(WebhookAdded).GetEventType().Id;

    static EventSequenceNumber PositionOf(Cratis.Monads.Option<AppendedEvent> last) =>
        last.TryGetValue(out var lifecycle) ? lifecycle.Context.SequenceNumber : EventSequenceNumber.BeforeFirst;

    async Task<EventSequenceNumber> ReadLifecyclePositionAndCheckObserverHistory(EventStoreName eventStore, WebhookDefinition requested)
    {
        var last = await GetSystemLog(eventStore).TryGetLastInstanceOfAny(requested.Identifier.Value, _lifecycleEventTypeIds);

        // A registration that is in the log but not yet applied to the read model is pending: fail closed rather than
        // guess which state the webhook will end up in.
        if (last.TryGetValue(out var appended) && IsWebhookAdded(appended))
        {
            throw new WebhookLifecycleChangedDuringRegistration(requested.Identifier);
        }

        if (await storage.GetEventStore(eventStore).Observers.Has(requested.Identifier))
        {
            throw new EnvelopeWebhookIdentifierHasObserverHistory(requested.Identifier);
        }

        return PositionOf(last);
    }

    async Task<bool> AdmitEnvelope(IEventSequence eventSequence, string id, WebhookAdded added, WebhookAdmissionContext context)
    {
        if (!context.Positions.TryGetValue(id, out var position))
        {
            throw new WebhookLifecycleChangedDuringRegistration(id);
        }

        var result = await AppendWebhookAdded(eventSequence, id, added, position);
        if (result.HasConcurrencyViolations) throw new WebhookLifecycleChangedDuringRegistration(id);
        if (!result.IsSuccess) throw new WebhookRegistrationFailed(id, typeof(WebhookAdded));
        return true;
    }

    async Task<bool> AdmitLegacy(IEventSequence eventSequence, string id, WebhookAdded added, EventStoreName eventStore)
    {
        var log = GetSystemLog(eventStore);
        var last = await log.TryGetLastInstanceOfAny(id, _lifecycleEventTypeIds);
        if (last.TryGetValue(out var lastAppended) && IsWebhookAdded(lastAppended))
        {
            return SkipIfIdentical(id, added, lastAppended);
        }

        var result = await AppendWebhookAdded(eventSequence, id, added, PositionOf(last));
        if (result.IsSuccess) return true;
        if (!result.HasConcurrencyViolations) throw new WebhookRegistrationFailed(id, typeof(WebhookAdded));

        // Another writer registered or removed the webhook meanwhile. Look once; there is no retry loop.
        var current = await log.TryGetLastInstanceOfAny(id, _lifecycleEventTypeIds);
        if (current.TryGetValue(out var currentAppended) && IsWebhookAdded(currentAppended))
        {
            return SkipIfIdentical(id, added, currentAppended);
        }

        throw new WebhookRegistrationFailed(id, typeof(WebhookAdded));
    }

    bool SkipIfIdentical(string id, WebhookAdded requested, AppendedEvent registered)
    {
        var stored = (WebhookAdded)eventSerializer.Deserialize(registered);
        if (stored.DeliveryFormat != requested.DeliveryFormat)
        {
            throw new WebhookDeliveryFormatCannotBeChanged(id, stored.DeliveryFormat, requested.DeliveryFormat);
        }

        logger.IdenticalWebhookAlreadyRegistered(id);
        return false;
    }

    Task<AppendResult> AppendWebhookAdded(IEventSequence eventSequence, string id, WebhookAdded added, EventSequenceNumber position) =>
        eventSequence.Append(
            EventSourceType.Default,
            id,
            EventStreamType.All,
            EventStreamId.Default,
            typeof(WebhookAdded).GetEventType(),
            eventSerializer.Serialize(added),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            new ConcurrencyScope(position, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: _lifecycleEventTypes));

    Storage.EventSequences.IEventSequenceStorage GetSystemLog(EventStoreName eventStore) =>
        storage.GetEventStore(eventStore).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System);
}
