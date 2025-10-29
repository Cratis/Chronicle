// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Events;

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

    static Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target) =>
        new()
        {
            Authentication = target.Authentication.ToContract(),
            Headers = target.Headers,
            Url = target.Url,
            BearerToken = target.BearerToken,
            Password = target.Password,
            Username = target.Username
        };

    static Cratis.Chronicle.Contracts.Observation.Webhooks.AuthenticationType
        ToContract(this AuthenticationType type) =>
        type switch
        {
            AuthenticationType.Basic => Contracts.Observation.Webhooks.AuthenticationType.Basic,
            AuthenticationType.Bearer => Contracts.Observation.Webhooks.AuthenticationType.Bearer,
            _ => Contracts.Observation.Webhooks.AuthenticationType.None
        };

    static WebhookTarget ToApi(this Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget target) =>
        new(
            target.Url,
            target.Authentication.ToApi(),
            target.Username,
            target.Password,
            target.BearerToken,
            target.Headers.ToDictionary(_ => _.Key, _ => _.Value));

    static AuthenticationType ToApi(this Cratis.Chronicle.Contracts.Observation.Webhooks.AuthenticationType type) =>
        type switch
        {
            Contracts.Observation.Webhooks.AuthenticationType.Basic => AuthenticationType.Basic,
            Contracts.Observation.Webhooks.AuthenticationType.Bearer => AuthenticationType.Bearer,
            _ => AuthenticationType.None
        };
}