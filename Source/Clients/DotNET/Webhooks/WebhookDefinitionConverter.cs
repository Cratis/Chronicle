// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents a converter for converting between webhook types.
/// </summary>
internal static class WebhookDefinitionConverter
{
    /// <summary>
    /// Converts a <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to a <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/>.</returns>
    internal static Contracts.Observation.Webhooks.WebhookDefinition ToContract(this WebhookDefinition definition) =>
        new()
        {
            EventSequenceId = definition.EventSequenceId.Value,
            EventTypes = definition.EventTypes.Select(_ => _.ToContract()).ToArray(),
            Identifier = definition.Identifier.Value,
            IsActive = definition.IsActive,
            IsReplayable = definition.IsReplayable,
            Target = definition.Target.ToContract()
        };

    static Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target)
    {
        var contractTarget = new Contracts.Observation.Webhooks.WebhookTarget
        {
            Url = target.Url.Value,
            Headers = target.Headers.ToDictionary(_ => _.Key, _ => _.Value)
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
}
