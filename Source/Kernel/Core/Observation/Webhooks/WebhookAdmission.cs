// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Validates delivery formats and writes registration changes against the authoritative webhook lifecycle.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for reading the system event sequence and observers.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and reading back registration events.</param>
class WebhookAdmission(IGrainFactory grainFactory, IStorage storage, IEventSerializer eventSerializer)
{
    static readonly EventType[] _lifecycleEventTypes = [typeof(WebhookAdded).GetEventType(), typeof(WebhookRemoved).GetEventType()];
    static readonly EventTypeId[] _lifecycleEventTypeIds = [.. _lifecycleEventTypes.Select(_ => _.Id)];

    /// <summary>
    /// Validates the delivery formats of a request, before anything is appended.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the webhooks belong to.</param>
    /// <param name="webhooks">The requested webhook definitions.</param>
    /// <returns>The context to write the registrations with.</returns>
    /// <exception cref="WebhookDeliveryFormatNotSupported">A requested delivery format is not supported.</exception>
    /// <exception cref="WebhookDeliveryFormatCannotBeChanged">The delivery format of an existing webhook is changed.</exception>
    public async Task<WebhookAdmissionContext> Validate(EventStoreName eventStore, IEnumerable<ContractWebhookDefinition> webhooks)
    {
        var requested = webhooks.Select(_ => _.ToChronicle()).ToArray();
        foreach (var webhook in requested.Where(_ => _.DeliveryFormat != WebhookDeliveryFormat.Legacy))
        {
            throw new WebhookDeliveryFormatNotSupported(webhook.Identifier, webhook.DeliveryFormat);
        }

        var existingWebhooks = (await grainFactory.GetGrain<IWebhooks>(eventStore).GetWebhookDefinitions()).ToArray();
        foreach (var webhook in requested)
        {
            var existing = existingWebhooks.FirstOrDefault(_ => _.Identifier == webhook.Identifier);
            if (existing is not null)
            {
                EnsureSameFormat(webhook, existing.DeliveryFormat);
            }
        }

        return new WebhookAdmissionContext(eventStore);
    }

    /// <summary>
    /// Rechecks the lifecycle and writes every registration branch, including unchanged definitions.
    /// </summary>
    /// <remarks>
    /// Legacy-over-legacy writes deliberately remain unfenced and last-write-wins. Non-legacy writes use a lifecycle
    /// scope: the system event sequence checks it and appends in one turn. Authorization after our own add must not
    /// use that scope, since our add has advanced the lifecycle. Envelope admission is dormant until dispatch supports it.
    /// </remarks>
    /// <param name="eventSequence">The system event sequence.</param>
    /// <param name="webhook">The requested webhook.</param>
    /// <param name="context">The registration request context.</param>
    /// <param name="compare">Compares the definition and builds its events and optional authorization change.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="WebhookDeliveryFormatCannotBeChanged">The log holds a different delivery format.</exception>
    /// <exception cref="EnvelopeWebhookIdentifierHasObserverHistory">An envelope identifier has observer history.</exception>
    /// <exception cref="WebhookLifecycleChangedDuringRegistration">The envelope lifecycle is pending or changes during the write.</exception>
    /// <exception cref="WebhookRegistrationFailed">An append fails.</exception>
    public async Task Write(
        IEventSequence eventSequence,
        WebhookDefinition webhook,
        WebhookAdmissionContext context,
        Func<Task<(IEnumerable<object> Events, WebhookAuthorization? Authorization)>> compare)
    {
        var id = webhook.Identifier.Value;
        var last = await GetSystemLog(context.EventStore).TryGetLastInstanceOfAny(id, _lifecycleEventTypeIds);
        var registered = last.TryGetValue(out var lifecycle) && lifecycle.Context.EventType.Id == typeof(WebhookAdded).GetEventType().Id;
        if (registered)
        {
            EnsureSameFormat(webhook, ((WebhookAdded)eventSerializer.Deserialize(lifecycle)).DeliveryFormat);
        }

        if (webhook.UsesDeliveryEnvelope())
        {
            if (context.Admitted.Contains(id)) return;
            await CheckEnvelopeIdentity(context.EventStore, webhook, registered);
        }

        var position = last.TryGetValue(out var appended) ? appended.Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        var scope = webhook.DeliveryFormat == WebhookDeliveryFormat.Legacy ? null : new ConcurrencyScope(
            position, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: _lifecycleEventTypes);
        var changes = await compare();
        var added = false;
        foreach (var @event in changes.Events)
        {
            await Append(eventSequence, id, @event, scope);
            added |= @event is WebhookAdded;
        }

        if (changes.Authorization is not null)
        {
            await AppendAuthorization(eventSequence, id, changes.Authorization, added ? null : scope);
        }

        if (webhook.UsesDeliveryEnvelope()) context.Admitted.Add(id);
    }

    static void EnsureSameFormat(WebhookDefinition requested, WebhookDeliveryFormat stored)
    {
        if (stored != requested.DeliveryFormat)
        {
            throw new WebhookDeliveryFormatCannotBeChanged(requested.Identifier, stored, requested.DeliveryFormat);
        }
    }

    static void EnsureAppended(bool success, bool lifecycleChanged, string id, Type eventType)
    {
        if (lifecycleChanged) throw new WebhookLifecycleChangedDuringRegistration(id);
        if (!success) throw new WebhookRegistrationFailed(id, eventType);
    }

    async Task CheckEnvelopeIdentity(EventStoreName eventStore, WebhookDefinition webhook, bool registered)
    {
        if (registered)
        {
            var existing = await grainFactory.GetGrain<IWebhooks>(eventStore).GetWebhookDefinitions();
            if (!existing.Any(_ => _.Identifier == webhook.Identifier))
            {
                throw new WebhookLifecycleChangedDuringRegistration(webhook.Identifier);
            }
        }
        else if (await storage.GetEventStore(eventStore).Observers.Has(webhook.Identifier))
        {
            throw new EnvelopeWebhookIdentifierHasObserverHistory(webhook.Identifier);
        }
    }

    Task AppendAuthorization(IEventSequence sequence, string id, WebhookAuthorization authorization, ConcurrencyScope? scope) =>
        authorization.Match(
            basic => Append(sequence, id, new BasicAuthorizationSetForWebhook(basic.Username, basic.Password), scope),
            bearer => Append(sequence, id, new BearerTokenAuthorizationSetForWebhook(bearer.Token), scope),
            oauth => AppendOAuth(sequence, id, oauth, scope),
            none => Task.CompletedTask,
            managedIdentity => Append(sequence, id, new ManagedIdentityAuthorizationSetForWebhook(managedIdentity.ClientId, managedIdentity.Scope), scope));

    async Task AppendOAuth(IEventSequence sequence, string id, OAuthAuthorization oauth, ConcurrencyScope? scope)
    {
        var authorization = new OAuthAuthorizationSetForWebhook(oauth.Authority, oauth.ClientId, oauth.ClientSecret);
        if (string.IsNullOrEmpty(oauth.Scope))
        {
            await Append(sequence, id, authorization, scope);
            return;
        }

        var events = new EventToAppend[]
        {
            new(
                EventSourceType.Default,
                id,
                EventStreamType.All,
                EventStreamId.Default,
                typeof(OAuthAuthorizationSetForWebhook).GetEventType(),
                [],
                new JsonObject { ["authority"] = oauth.Authority.Value, ["clientId"] = oauth.ClientId.Value, ["clientSecret"] = oauth.ClientSecret.Value }),
            new(
                EventSourceType.Default,
                id,
                EventStreamType.All,
                EventStreamId.Default,
                typeof(OAuthScopeSetForWebhook).GetEventType(),
                [],
                new JsonObject { ["scope"] = oauth.Scope })
        };
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        if (scope is not null) scopes[id] = scope;
        var result = await sequence.AppendMany(events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(scopes));
        EnsureAppended(result.IsSuccess, scope is not null && result.HasConcurrencyViolations, id, typeof(OAuthScopeSetForWebhook));
    }

    async Task Append(IEventSequence sequence, string id, object @event, ConcurrencyScope? scope)
    {
        var result = scope is null
            ? await sequence.Append(id, @event)
            : await sequence.Append(
                EventSourceType.Default,
                id,
                EventStreamType.All,
                EventStreamId.Default,
                @event.GetType().GetEventType(),
                eventSerializer.Serialize(@event),
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                scope);
        EnsureAppended(result.IsSuccess, scope is not null && result.HasConcurrencyViolations, id, @event.GetType());
    }

    Storage.EventSequences.IEventSequenceStorage GetSystemLog(EventStoreName eventStore) =>
        storage.GetEventStore(eventStore).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System);
}
