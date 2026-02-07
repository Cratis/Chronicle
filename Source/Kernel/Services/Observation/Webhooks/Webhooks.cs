// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Observation.Webhooks;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;
using WebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Services.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooks"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for getting webhook definitions.</param>
/// <param name="webhookDefinitionComparer"><see cref="IWebhookDefinitionComparer"/> for comparing webhook definitions.</param>
/// <param name="encryption"><see cref="IEncryption"/> for encrypting sensitive data.</param>
internal sealed class Webhooks(
    IGrainFactory grainFactory,
    IStorage storage,
    IWebhookDefinitionComparer webhookDefinitionComparer,
    IEncryption encryption) : IWebhooks
{
    /// <inheritdoc/>
    public async Task Add(AddWebhooks request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetEventLog();
        var webhooksManager = grainFactory.GetGrain<IWebhooksManager>(request.EventStore);

        foreach (var webhook in request.Webhooks)
        {
            var chronicleWebhook = webhook.ToChronicle();
            var encryptedWebhook = EncryptWebhookSecrets(chronicleWebhook);
            var webhookKey = new WebhookKey(chronicleWebhook.Identifier, request.EventStore);

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
                    encryptedWebhook.IsActive);

                await eventSequence.Append(webhook.Identifier, addedEvent);
                await AppendAuthorizationEvent(eventSequence, webhook.Identifier, encryptedWebhook.Target.Authorization);
            }
            else if (compareResult.Result == WebhookDefinitionCompareResult.Different && compareResult.ChangedProperties is not null)
            {
                var changedProperties = compareResult.ChangedProperties;

                if (changedProperties.EventTypesChanged)
                {
                    await eventSequence.Append(webhook.Identifier, new EventTypesSetForWebhook(encryptedWebhook.EventTypes));
                }

                if (changedProperties.TargetUrlChanged)
                {
                    await eventSequence.Append(webhook.Identifier, new TargetUrlSetForWebhook(encryptedWebhook.Target.Url));
                }

                if (changedProperties.TargetHeadersChanged)
                {
                    await eventSequence.Append(webhook.Identifier, new TargetHeadersSetForWebhook(encryptedWebhook.Target.Headers));
                }

                if (changedProperties.AuthorizationChanged)
                {
                    await AppendAuthorizationEvent(eventSequence, webhook.Identifier, encryptedWebhook.Target.Authorization);
                }
            }

            // If compareResult is Same, no event is appended
        }
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveWebhooks request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetEventLog();

        foreach (var webhookId in request.Webhooks)
        {
            var @event = new WebhookRemoved();
            await eventSequence.Append(webhookId, @event);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WebhookDefinition>> GetWebhooks(GetWebhooksRequest request)
    {
        var definitions = await storage.GetEventStore(request.EventStore).Webhooks.GetAll();
        return definitions.Select(definition => definition.ToContract());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<WebhookDefinition>> ObserveWebhooks(GetWebhooksRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore)
            .Webhooks
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(definitions => definitions.Select(definition => definition.ToContract()).ToList());

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
                new ClientSecret(encryption.Encrypt(oauth.ClientSecret.Value))),
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
            async basic => await eventSequence.Append(webhookId, new BasicAuthorizationSetForWebhook(basic.Username, basic.Password)),
            async bearer => await eventSequence.Append(webhookId, new BearerTokenAuthorizationSetForWebhook(bearer.Token)),
            async oauth => await eventSequence.Append(webhookId, new OAuthAuthorizationSetForWebhook(oauth.Authority, oauth.ClientId, oauth.ClientSecret)),
            async none => await Task.CompletedTask);
    }
}
