// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using WebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Registers webhooks, and tests the endpoints and authorizations they point at.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="webhookDefinitionComparer"><see cref="IWebhookDefinitionComparer"/> for comparing webhook definitions.</param>
/// <param name="encryption"><see cref="IEncryption"/> for encrypting sensitive data.</param>
/// <param name="oauthClient"><see cref="IOAuthClient"/> for testing OAuth authorization.</param>
/// <param name="webhookMediator"><see cref="IWebhookMediator"/> for testing webhook endpoints.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <param name="storage">Storage for validating registered event generations.</param>
/// <remarks>
/// Registering a webhook is a diff, not a write: the definition is compared with what is stored and only what
/// actually changed becomes an event. Testing lives here too, because a validator has to ask the same questions
/// - can we reach it, can we get a token - before the command is allowed to run at all.
/// </remarks>
public sealed class WebhookRegistrar(
    IGrainFactory grainFactory,
    IWebhookDefinitionComparer webhookDefinitionComparer,
    IEncryption encryption,
    IOAuthClient oauthClient,
    IWebhookMediator webhookMediator,
    IOptions<ChronicleOptions> options,
    IStorage storage)
{
    const string WebhookTestPartitionKey = "test";
    readonly TimeSpan _webhookTestTimeout = TimeSpan.FromSeconds(options.Value.Webhooks.TestTimeoutSeconds);

    /// <summary>
    /// Adds or updates a set of webhook definitions for an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the webhooks belong to.</param>
    /// <param name="webhooks">The webhook definitions to add or update.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="WebhookRegistrationFailed">Thrown when an event recording the registration could not be appended.</exception>
    /// <exception cref="ObserverPinsMultipleGenerationsOfEventType">The webhook pins multiple generations of one type.</exception>
    /// <exception cref="EventTypeGenerationNotRegisteredForObserver">A pinned generation is not registered.</exception>
    internal async Task Add(EventStoreName eventStore, IEnumerable<WebhookDefinition> webhooks)
    {
        webhooks = webhooks.ToArray();

        // Validate the entire request before appending any registration changes.
        await ValidatePins(eventStore, webhooks);

        var eventSequence = grainFactory.GetSystemEventSequence(eventStore);
        var webhooksManager = grainFactory.GetGrain<Chronicle.Observation.Webhooks.IWebhooks>(eventStore);

        foreach (var webhook in webhooks)
        {
            var chronicleWebhook = webhook.ToChronicle();
            var encryptedWebhook = EncryptWebhookSecrets(chronicleWebhook);
            var webhookKey = new WebhookKey(chronicleWebhook.Identifier, eventStore);

            var existingWebhooks = await webhooksManager.GetWebhookDefinitions();
            var existingWebhook = existingWebhooks.FirstOrDefault(w => w.Identifier == chronicleWebhook.Identifier);

            var compareResult = await webhookDefinitionComparer.Compare(
                webhookKey,
                existingWebhook ?? encryptedWebhook,
                encryptedWebhook);

            if (compareResult.Result == WebhookDefinitionCompareResult.New)
            {
                var addedEvent = new WebhookAdded(
                    encryptedWebhook.Owner,
                    encryptedWebhook.EventSequenceId,
                    encryptedWebhook.EventTypes,
                    encryptedWebhook.Target.Url,
                    encryptedWebhook.Target.Headers,
                    encryptedWebhook.IsReplayable,
                    encryptedWebhook.IsActive)
                {
                    GenerationDelivery = encryptedWebhook.GenerationDelivery
                };

                await Append(eventSequence, webhook.Identifier, addedEvent);
                await AppendAuthorizationEvent(eventSequence, webhook.Identifier, encryptedWebhook.Target.Authorization);
            }
            else if (compareResult.Result == WebhookDefinitionCompareResult.Different && compareResult.ChangedProperties is not null)
            {
                var changedProperties = compareResult.ChangedProperties;

                await AppendGenerationChanges(eventSequence, encryptedWebhook, changedProperties);

                if (changedProperties.TargetUrlChanged)
                {
                    await Append(eventSequence, webhook.Identifier, new TargetUrlSetForWebhook(encryptedWebhook.Target.Url));
                }

                if (changedProperties.TargetHeadersChanged)
                {
                    await Append(eventSequence, webhook.Identifier, new TargetHeadersSetForWebhook(encryptedWebhook.Target.Headers));
                }

                if (changedProperties.AuthorizationChanged)
                {
                    await AppendAuthorizationEvent(eventSequence, webhook.Identifier, encryptedWebhook.Target.Authorization);
                }
            }

            // If compareResult is Same, no event is appended
        }
    }

    /// <summary>
    /// Removes a set of webhooks from an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the webhooks belong to.</param>
    /// <param name="webhooks">The identifiers of the webhooks to remove.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="WebhookRegistrationFailed">Thrown when a removal could not be appended.</exception>
    /// <remarks>
    /// The removals are appended together rather than one at a time, so the order in which they land in the system
    /// event sequence relative to each other is no longer the order they appear in the request. Each removal targets
    /// its own event source - the webhook identifier - so per-stream ordering is unaffected, and observers partition by
    /// event source, so nothing observes the relative order of two different webhooks being removed.
    /// </remarks>
    internal async Task Remove(EventStoreName eventStore, IEnumerable<string> webhooks)
    {
        var eventSequence = grainFactory.GetSystemEventSequence(eventStore);
        await Task.WhenAll(webhooks.Select(webhookId => Append(eventSequence, webhookId, new WebhookRemoved())));
    }

    /// <summary>
    /// Tests whether an access token can be acquired from an OAuth authority.
    /// </summary>
    /// <param name="authority">The authority to acquire the token from.</param>
    /// <param name="clientId">The client identifier to authenticate with.</param>
    /// <param name="clientSecret">The client secret to authenticate with.</param>
    /// <param name="scope">The optional scope to request, empty for none.</param>
    /// <returns>The <see cref="WebhookTestResult"/> of the attempt.</returns>
    internal async Task<WebhookTestResult> TestOAuthAuthorization(string authority, string clientId, string clientSecret, string scope)
    {
        var authorization = new OAuthAuthorization(
            new Authority(authority),
            new ClientId(clientId),
            new ClientSecret(clientSecret))
        {
            Scope = scope ?? string.Empty
        };

        try
        {
            var tokenInfo = await oauthClient.AcquireToken(authorization);
            return new WebhookTestResult
            {
                Success = !string.IsNullOrEmpty(tokenInfo.AccessToken),
                ErrorMessage = string.IsNullOrEmpty(tokenInfo.AccessToken) ? "Failed to acquire access token" : string.Empty
            };
        }
        catch (Exception ex)
        {
            return new WebhookTestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Tests whether a webhook target can be reached.
    /// </summary>
    /// <param name="target">The webhook target to test.</param>
    /// <returns>The <see cref="WebhookTestResult"/> of the attempt.</returns>
    internal async Task<WebhookTestResult> TestWebhook(Contracts.Observation.Webhooks.WebhookTarget target)
    {
        string? accessToken = null;

        if (target.Authorization?.Value2 is { } oAuthContract)
        {
            var oAuth = new OAuthAuthorization(
                new Authority(oAuthContract.Authority),
                new ClientId(oAuthContract.ClientId),
                new ClientSecret(oAuthContract.ClientSecret)) { Scope = oAuthContract.Scope };

            try
            {
                var tokenInfo = await oauthClient.AcquireToken(oAuth);
                if (string.IsNullOrEmpty(tokenInfo.AccessToken))
                {
                    return new WebhookTestResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to acquire OAuth access token."
                    };
                }

                accessToken = tokenInfo.AccessToken;
            }
            catch (Exception ex)
            {
                return new WebhookTestResult
                {
                    Success = false,
                    ErrorMessage = $"OAuth authorization failed: {ex.Message}"
                };
            }
        }

        var chronicleTarget = target.ToChronicle();
        var result = await webhookMediator.OnNext(
            chronicleTarget,
            new Key(WebhookTestPartitionKey, ArrayIndexers.NoIndexers),
            [],
            accessToken,
            _webhookTestTimeout);

        if (result.IsSuccess)
        {
            return new WebhookTestResult { Success = true };
        }

        result.TryGetException(out var exception);
        return new WebhookTestResult
        {
            Success = false,
            ErrorMessage = exception?.Message ?? "Unable to connect to webhook endpoint."
        };
    }

    async Task ValidatePins(EventStoreName eventStore, IEnumerable<WebhookDefinition> webhooks)
    {
        foreach (var webhook in webhooks.Where(_ => _.GenerationDelivery == Contracts.Observation.EventGenerationDelivery.Pinned))
        {
            await ValidatePins(eventStore, webhook);
        }
    }

    async Task ValidatePins(EventStoreName eventStore, WebhookDefinition webhook)
    {
        var pins = webhook.EventTypes.Select(_ => _.ToChronicle()).ToArray();
        var duplicate = pins.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(pin => pin.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }

        var eventTypes = storage.GetEventStore(eventStore).EventTypes;
        foreach (var pin in pins)
        {
            if (!await eventTypes.HasFor(pin.Id, pin.Generation) || !(await eventTypes.GetDefinition(pin.Id)).Generations.Any(_ => _.Generation == pin.Generation))
            {
                throw new EventTypeGenerationNotRegisteredForObserver(pin);
            }
        }
    }

    async Task AppendGenerationChanges(
        IEventSequence eventSequence,
        Concepts.Observation.Webhooks.WebhookDefinition webhook,
        WebhookDefinitionChangedProperties changedProperties)
    {
        if (changedProperties.GenerationDeliveryChanged)
        {
            await Append(eventSequence, webhook.Identifier, new GenerationDeliverySetForWebhook(webhook.GenerationDelivery, webhook.EventTypes));
        }
        else if (changedProperties.EventTypesChanged)
        {
            await Append(eventSequence, webhook.Identifier, new EventTypesSetForWebhook(webhook.EventTypes));
        }
    }

    Concepts.Observation.Webhooks.WebhookDefinition EncryptWebhookSecrets(Concepts.Observation.Webhooks.WebhookDefinition definition)
    {
        var encryptedAuthorization = definition.Target.Authorization.Match(
            basic => (WebhookAuthorization)new BasicAuthorization(
                basic.Username,
                new Password(encryption.Encrypt(basic.Password.Value))),
            bearer => (WebhookAuthorization)new BearerTokenAuthorization(
                new Token(encryption.Encrypt(bearer.Token.Value))),
            oauth => (WebhookAuthorization)new OAuthAuthorization(
                oauth.Authority,
                oauth.ClientId,
                new ClientSecret(encryption.Encrypt(oauth.ClientSecret.Value))) { Scope = oauth.Scope },
            none => WebhookAuthorization.None);

        var encryptedTarget = new Concepts.Observation.Webhooks.WebhookTarget(
            definition.Target.Url,
            encryptedAuthorization,
            definition.Target.Headers);

        return definition with { Target = encryptedTarget };
    }

    async Task AppendAuthorizationEvent(IEventSequence eventSequence, string webhookId, WebhookAuthorization authorization)
    {
        await authorization.Match(
            basic => Append(eventSequence, webhookId, new BasicAuthorizationSetForWebhook(basic.Username, basic.Password)),
            bearer => Append(eventSequence, webhookId, new BearerTokenAuthorizationSetForWebhook(bearer.Token)),
            oauth => AppendOAuth(eventSequence, webhookId, oauth),
            none => Task.CompletedTask);
    }

    async Task AppendOAuth(IEventSequence sequence, string webhookId, OAuthAuthorization oauth)
    {
        var authorization = new OAuthAuthorizationSetForWebhook(oauth.Authority, oauth.ClientId, oauth.ClientSecret);
        if (string.IsNullOrEmpty(oauth.Scope))
        {
            await Append(sequence, webhookId, authorization);
            return;
        }

        var events = new EventToAppend[]
        {
            new(
                EventSourceType.Default,
                webhookId,
                EventStreamType.All,
                EventStreamId.Default,
                typeof(OAuthAuthorizationSetForWebhook).GetEventType(),
                [],
                new JsonObject { ["authority"] = oauth.Authority.Value, ["clientId"] = oauth.ClientId.Value, ["clientSecret"] = oauth.ClientSecret.Value }),
            new(
                EventSourceType.Default,
                webhookId,
                EventStreamType.All,
                EventStreamId.Default,
                typeof(OAuthScopeSetForWebhook).GetEventType(),
                [],
                new JsonObject { ["scope"] = oauth.Scope })
        };
        var result = await sequence.AppendMany(events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
        if (!result.IsSuccess)
        {
            throw new WebhookRegistrationFailed(webhookId, typeof(OAuthScopeSetForWebhook));
        }
    }

    /// <summary>
    /// Appends an event recording part of a webhook registration, failing the registration when the append fails.
    /// </summary>
    /// <remarks>
    /// A registration is a series of appends, and a failed one is not an exception from the event sequence but a
    /// failed result. Ignoring it reported the registration as complete with only part of it stored - a webhook
    /// without its authorization, for instance.
    /// </remarks>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append to.</param>
    /// <param name="webhookId">The identifier of the webhook, used as event source.</param>
    /// <param name="event">The event to append.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="WebhookRegistrationFailed">Thrown when the append did not succeed.</exception>
    async Task Append(IEventSequence eventSequence, string webhookId, object @event)
    {
        var result = await eventSequence.Append(webhookId, @event);
        if (!result.IsSuccess)
        {
            throw new WebhookRegistrationFailed(webhookId, @event.GetType());
        }
    }
}
