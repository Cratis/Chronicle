// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

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
            webhookDefinition.WebhookId,
            WebhookOwner.Client,
            webhookDefinition.EventSequenceId,
            webhookDefinition.EventTypes.Select(_ => _.ToChronicle()),
            webhookDefinition.Target.ToChronicle(),
            webhookDefinition.IsReplayable);

    static WebhookTarget ToChronicle(this Contracts.Observation.Webhooks.WebhookTarget target) =>
        new(
            target.Url,
            target.Authentication.ToChronicle(),
            target.Username,
            target.Password,
            target.BearerToken,
            target.Headers.AsReadOnly());

    static AuthenticationType ToChronicle(this Contracts.Observation.Webhooks.AuthenticationType authenticationType) =>
        authenticationType switch
        {
            Contracts.Observation.Webhooks.AuthenticationType.None => AuthenticationType.None,
            Contracts.Observation.Webhooks.AuthenticationType.Basic => AuthenticationType.Basic,
            Contracts.Observation.Webhooks.AuthenticationType.Bearer => AuthenticationType.Bearer,
            _ => (AuthenticationType)authenticationType
        };
}
