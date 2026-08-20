// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Webhooks;

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
            webhookDefinition.EventTypes.Select(_ => _.ToChronicle()).ToArray(),
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

    /// <summary>
    /// Convert from <see cref="Contracts.Observation.Webhooks.WebhookTarget"/> to <see cref="WebhookTarget"/>.
    /// </summary>
    /// <param name="target"><see cref="Contracts.Observation.Webhooks.WebhookTarget"/> to convert from.</param>
    /// <returns>Converted <see cref="WebhookTarget"/>.</returns>
    internal static WebhookTarget ToChronicle(this Contracts.Observation.Webhooks.WebhookTarget target)
    {
        var authorization = target.Authorization switch
        {
            null => WebhookAuthorization.None,
            var auth when auth.Value0 is not null => new BasicAuthorization(auth.Value0.Username, auth.Value0.Password),
            var auth when auth.Value1 is not null => new BearerTokenAuthorization(auth.Value1.Token),
            var auth when auth.Value2 is not null => new OAuthAuthorization(
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

    /// <summary>
    /// Convert from <see cref="WebhookTarget"/> to <see cref="Contracts.Observation.Webhooks.WebhookTarget"/>.
    /// </summary>
    /// <param name="target"><see cref="WebhookTarget"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Observation.Webhooks.WebhookTarget"/>.</returns>
    internal static Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target)
    {
        var contractTarget = new Contracts.Observation.Webhooks.WebhookTarget
        {
            Headers = target.Headers.ToDictionary(),
            Url = target.Url
        };

        target.Authorization.Switch(
            basic => contractTarget.Authorization = new(new Contracts.Security.BasicAuthorization
            {
                Username = basic.Username,
                Password = basic.Password
            }),
            bearer => contractTarget.Authorization = new(new Contracts.Security.BearerTokenAuthorization
            {
                Token = bearer.Token
            }),
            oauth => contractTarget.Authorization = new(new Contracts.Security.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = oauth.ClientSecret
            }),
            none => { });

        return contractTarget;
    }

    /// <summary>
    /// Converts stored webhook definitions into the read model the webhook queries answer with.
    /// </summary>
    /// <param name="definitions">The stored definitions.</param>
    /// <returns>The definitions as read models.</returns>
    internal static IEnumerable<WebhookDetails> ToReadModel(this IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition> definitions) =>
        [.. definitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a stored webhook definition into the read model the webhook queries answer with.
    /// </summary>
    /// <param name="definition">The stored definition.</param>
    /// <returns>The definition as a read model.</returns>
    internal static WebhookDetails ToReadModel(this Concepts.Observation.Webhooks.WebhookDefinition definition)
    {
        var contract = definition.ToContract();
        return new(
            contract.Identifier,
            contract.Identifier,
            contract.Target.Url,
            contract.EventSequenceId,
            contract.EventTypes,
            definition.Target.Authorization.ToAuthorizationType(),
            contract.Target.Headers,
            contract.IsReplayable,
            contract.IsActive);
    }

    /// <summary>
    /// Resolves which kind of authorization a webhook target carries.
    /// </summary>
    /// <param name="authorization">The <see cref="WebhookAuthorization"/> to resolve from.</param>
    /// <returns>The matching <see cref="Contracts.Security.AuthorizationType"/>.</returns>
    internal static Contracts.Security.AuthorizationType ToAuthorizationType(this WebhookAuthorization authorization) =>
        authorization.Match(
            _ => Contracts.Security.AuthorizationType.Basic,
            _ => Contracts.Security.AuthorizationType.Bearer,
            _ => Contracts.Security.AuthorizationType.OAuth,
            _ => Contracts.Security.AuthorizationType.None);
}
