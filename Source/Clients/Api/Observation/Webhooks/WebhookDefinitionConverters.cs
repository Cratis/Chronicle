// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary
#pragma warning disable IDE0001 // Name can be simplified

using Cratis.Chronicle.Api.Events;
using OneOf.Types;

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Extension methods for working with <see cref="WebhookDefinition"/>.
/// </summary>
internal static class WebhookDefinitionConverters
{
    /// <summary>
    /// Converts from <see cref="Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition"/> to <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The definition to convert.</param>
    /// <returns>The converted definition.</returns>
    internal static WebhookDefinition ToApi(
        this Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition definition) =>
        new(
            definition.Identifier,
            definition.EventTypes.Select(_ => _.ToApi()),
            definition.Target.ToApi(),
            definition.EventSequenceId,
            definition.IsReplayable,
            definition.IsActive);

    /// <summary>
    /// Converts from <see cref="WebhookDefinition"/> to <see cref="Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/> to convert.</param>
    /// <returns>The converted definition.</returns>
    internal static Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition ToContract(
        this WebhookDefinition definition) =>
        new()
        {
            EventSequenceId = definition.EventSequenceId,
            IsReplayable = definition.IsReplayable,
            Identifier = definition.Identifier,
            IsActive = definition.IsActive,
            EventTypes = definition.EventTypes.Select(_ => _.ToContract()).ToList(),
            Target = definition.Target.ToContract()
        };

    static Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target)
    {
        var contractTarget = new Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget
        {
            Headers = target.Headers,
            Url = target.Url
        };

        target.Authorization.Switch(
            basic => contractTarget.Authorization = new(new Cratis.Chronicle.Contracts.Observation.Webhooks.BasicAuthorization
            {
                Username = basic.Username,
                Password = basic.Password
            }),
            bearer => contractTarget.Authorization = new(new Cratis.Chronicle.Contracts.Observation.Webhooks.BearerTokenAuthorization
            {
                Token = bearer.Token
            }),
            oauth => contractTarget.Authorization = new(new Cratis.Chronicle.Contracts.Observation.Webhooks.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = oauth.ClientSecret
            }),
            none => { });

        return contractTarget;
    }

    static WebhookTarget ToApi(this Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget target)
    {
        OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> authorization = target.Authorization switch
        {
            null => default(OneOf.Types.None),
            var auth when auth.Value0 is not null => new BasicAuthorization(auth.Value0.Username, auth.Value0.Password),
            var auth when auth.Value1 is not null => new BearerTokenAuthorization(auth.Value1.Token),
            var auth when auth.Value2 is not null => new OAuthAuthorization(
                auth.Value2.Authority,
                auth.Value2.ClientId,
                auth.Value2.ClientSecret),
            _ => default(OneOf.Types.None)
        };

        return new(
            target.Url,
            authorization,
            target.Headers.ToDictionary(_ => _.Key, _ => _.Value));
    }
}
