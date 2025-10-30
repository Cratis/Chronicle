// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using OneOf.Types;

namespace Cratis.Chronicle.Services.Observation.Webhooks;

/// <summary>
/// Extension methods for converting between <see cref="WebhookDefinition"/> and <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/>.
/// </summary>
internal static class WebhookDefinitionConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="webhookDefinition"><see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="WebhookDefinition"/>.</returns>
    public static WebhookDefinition ToChronicle(this Contracts.Observation.Webhooks.WebhookDefinition webhookDefinition) =>
        new(
            webhookDefinition.Identifier,
            WebhookOwner.Client,
            webhookDefinition.EventSequenceId,
            webhookDefinition.EventTypes.Select(_ => _.ToChronicle()),
            webhookDefinition.Target.ToChronicle(),
            webhookDefinition.IsReplayable,
            webhookDefinition.IsActive);

    /// <summary>
    /// Convert from <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="webhookDefinition"><see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="WebhookDefinition"/>.</returns>
    public static Contracts.Observation.Webhooks.WebhookDefinition ToContract(this WebhookDefinition webhookDefinition) =>
        new()
        {
            Identifier = webhookDefinition.Identifier,
            EventSequenceId = webhookDefinition.EventSequenceId,
            EventTypes = webhookDefinition.EventTypes.Select(type => type.ToContract()).ToList(),
            IsActive = webhookDefinition.IsActive,
            IsReplayable = webhookDefinition.IsReplayable,
            Target = webhookDefinition.Target.ToContract()
        };

    static WebhookTarget ToChronicle(this Contracts.Observation.Webhooks.WebhookTarget target)
    {
        OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> authorization;
        if (target.BasicAuthorization is not null)
        {
            authorization = new BasicAuthorization(target.BasicAuthorization.Username, target.BasicAuthorization.Password);
        }
        else if (target.BearerTokenAuthorization is not null)
        {
            authorization = new BearerTokenAuthorization(target.BearerTokenAuthorization.Token);
        }
        else if (target.OAuthAuthorization is not null)
        {
            authorization = new OAuthAuthorization(
                target.OAuthAuthorization.Authority,
                target.OAuthAuthorization.ClientId,
                target.OAuthAuthorization.ClientSecret);
        }
        else
        {
            authorization = default(OneOf.Types.None);
        }

        return new(
            target.Url,
            authorization,
            target.Headers.AsReadOnly());
    }

    static Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target)
    {
        var contractTarget = new Contracts.Observation.Webhooks.WebhookTarget
        {
            Headers = target.Headers.ToDictionary(),
            Url = target.Url
        };

        target.Authorization.Switch(
            basic => contractTarget.BasicAuthorization = new Contracts.Observation.Webhooks.BasicAuthorization
            {
                Username = basic.Username,
                Password = basic.Password
            },
            bearer => contractTarget.BearerTokenAuthorization = new Contracts.Observation.Webhooks.BearerTokenAuthorization
            {
                Token = bearer.Token
            },
            oauth => contractTarget.OAuthAuthorization = new Contracts.Observation.Webhooks.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = oauth.ClientSecret
            },
            none => { });

        return contractTarget;
    }
}
