// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using OneOf.Types;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WebhookObserverSubscriber"/> class.
/// </remarks>
/// <param name="webhookMediator">The <see cref="IWebhookMediator"/>.</param>
/// <param name="oAuthClient">The <see cref="IOAuthClient"/>.</param>
/// <param name="encryption">The <see cref="IEncryption"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="generationRelease">The pinned generation release boundary.</param>
/// <param name="storage">Storage for the pinned event schemas.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Webhooks)]
public class WebhookObserverSubscriber(
    IWebhookMediator webhookMediator,
    IOAuthClient oAuthClient,
    IEncryption encryption,
    ILogger<WebhookObserverSubscriber> logger,
    IEventGenerationRelease generationRelease,
    IStorage storage) : Grain<WebhookDefinition>, IWebhookObserverSubscriber, INotifyWebhookDefinitionsChanged
{
    ObserverKey _key = ObserverKey.NotSet;
    AccessTokenInfo _accessTokenInfo = AccessTokenInfo.Empty;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        await EnsureAuthorized();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (State.GenerationDelivery == EventGenerationDelivery.Pinned || context.Metadata is EventGenerationDelivery.Pinned)
        {
            try
            {
                events = await ReleaseMismatches(events);
            }
            catch (Exception error)
            {
                return HandleError(error);
            }
        }

        await EnsureAuthorized();

        var accessToken = _accessTokenInfo.IsExpired ? null : _accessTokenInfo.AccessToken;
        var onNextResult = await webhookMediator.OnNext(State.Target, partition, events, accessToken);
        return onNextResult.Match(HandleSuccess, HandleError);

        ObserverSubscriberResult HandleSuccess(None none)
        {
            logger.SuccessfullyHandledAllEvents(_key);
            return ObserverSubscriberResult.Ok(events.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
        }
        ObserverSubscriberResult HandleError(Exception error)
        {
            logger.ErrorHandling(error, _key);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                error.GetAllMessages(),
                error.StackTrace ?? string.Empty);
        }
    }

    /// <inheritdoc/>
    public async Task OnWebhookDefinitionsChanged()
    {
        await ReadStateAsync();
        _accessTokenInfo = AccessTokenInfo.Empty;
        await EnsureAuthorized();
    }

    async Task<IEnumerable<AppendedEvent>> ReleaseMismatches(IEnumerable<AppendedEvent> events)
    {
        // A definition update can precede the observer rebind. Read the authoritative pins rather than
        // retaining a subscriber activation's old definition. Compatibility deliveries bypass this boundary.
        var webhook = GrainFactory.GetGrain<IWebhook>(new WebhookKey(_key.ObserverId, _key.EventStore));
        var definition = await webhook.GetDefinition();
        State = State with { EventTypes = definition.EventTypes, GenerationDelivery = definition.GenerationDelivery };
        if (State.GenerationDelivery != EventGenerationDelivery.Pinned)
        {
            return events;
        }

        var pins = State.EventTypes.Distinct().ToDictionary(_ => _.Id);
        var schemas = new Dictionary<EventType, EventTypeSchema>();
        var released = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            if (pins.TryGetValue(@event.Context.EventType.Id, out var pin) && pin != @event.Context.EventType)
            {
                if (!schemas.ContainsKey(pin))
                {
                    schemas[pin] = await storage.GetEventStore(_key.EventStore).EventTypes.GetFor(pin.Id, pin.Generation);
                }

                released.AddRange(await generationRelease.Release(_key.EventStore, [pin], schemas, [@event]));
            }
            else
            {
                // Matching generations were already released by the observer; do not release them twice.
                released.Add(@event);
            }
        }

        return released;
    }

    async Task EnsureAuthorized()
    {
        if (_accessTokenInfo.IsExpired)
        {
            _accessTokenInfo = await State.Target.Authorization.Match(
                basic => Task.FromResult(AccessTokenInfo.Empty),
                bearer => Task.FromResult(AccessTokenInfo.Empty),
                async oAuth =>
                {
                    var decryptedOAuth = oAuth with
                    {
                        ClientSecret = encryption.Decrypt(oAuth.ClientSecret)
                    };

                    var tokenInfo = await oAuthClient.AcquireToken(decryptedOAuth);
                    SetGrainLifetime(tokenInfo);
                    return tokenInfo;
                },
                none => Task.FromResult(AccessTokenInfo.Empty));
        }
    }

    void SetGrainLifetime(AccessTokenInfo tokenInfo)
    {
        if (!tokenInfo.IsExpired && tokenInfo.ExpiresAt != DateTimeOffset.MinValue)
        {
            var idleTime = tokenInfo.ExpiresAt - DateTimeOffset.UtcNow;
            if (idleTime > TimeSpan.Zero)
            {
                DelayDeactivation(idleTime);
            }
        }
    }
}
