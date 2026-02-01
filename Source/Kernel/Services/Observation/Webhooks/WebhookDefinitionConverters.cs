// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
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
            string.IsNullOrEmpty(webhookDefinition.EventSequenceId) ? EventSequenceId.Log : webhookDefinition.EventSequenceId,
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
        var authorization = target.Authorization switch
        {
            null => WebhookAuthorization.None,
            var auth when auth.Value0 is not null => new Concepts.Security.BasicAuthorization(auth.Value0.Username, auth.Value0.Password),
            var auth when auth.Value1 is not null => new Concepts.Security.BearerTokenAuthorization(auth.Value1.Token),
            var auth when auth.Value2 is not null => new Concepts.Security.OAuthAuthorization(
                auth.Value2.Authority,
                auth.Value2.ClientId,
                auth.Value2.ClientSecret),
            _ => WebhookAuthorization.None
        };
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
            basic => contractTarget.Authorization = new(new Contracts.Security.BasicAuthorization
            {
                Username = basic.Username.Value,
                Password = basic.Password.Value
            }),
            bearer => contractTarget.Authorization = new(new Contracts.Security.BearerTokenAuthorization
            {
                Token = bearer.Token.Value
            }),
            oauth => contractTarget.Authorization = new(new Contracts.Security.OAuthAuthorization
            {
                Authority = oauth.Authority.Value,
                ClientId = oauth.ClientId.Value,
                ClientSecret = oauth.ClientSecret.Value
            }),
            none => { });

        return contractTarget;
    }
}
