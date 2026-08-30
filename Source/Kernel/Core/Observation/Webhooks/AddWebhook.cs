// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the command for registering a single webhook from its individual settings.
/// </summary>
/// <param name="EventStore">The event store the webhook belongs to.</param>
/// <param name="Name">The name, which is also the identifier, of the webhook.</param>
/// <param name="Url">Where the webhook is called.</param>
/// <param name="EventSequenceId">The event sequence the webhook observes.</param>
/// <param name="EventTypes">The event types the webhook is called for.</param>
/// <param name="AuthorizationType">Which authorization the call carries.</param>
/// <param name="BasicUsername">The username for basic authorization.</param>
/// <param name="BasicPassword">The password for basic authorization.</param>
/// <param name="BearerToken">The token for bearer authorization.</param>
/// <param name="OAuthAuthority">The authority for OAuth authorization.</param>
/// <param name="OAuthClientId">The client identifier for OAuth authorization.</param>
/// <param name="OAuthClientSecret">The client secret for OAuth authorization.</param>
/// <param name="Headers">Additional headers the call carries.</param>
/// <param name="IsReplayable">Whether the webhook can be replayed.</param>
/// <param name="IsActive">Whether the webhook starts out delivering.</param>
/// <remarks>
/// This is the shape a form fills in: one webhook, its settings spread flat, and the authorization chosen by kind.
/// <see cref="AddWebhooks"/> is the shape a client registering what it already knows sends.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Webhooks)]
public record AddWebhook(
    EventStoreName EventStore,
    Concepts.Observation.Webhooks.WebhookId Name,
    string Url,
    string EventSequenceId,
    IEnumerable<Contracts.Events.EventType> EventTypes,
    AuthorizationType AuthorizationType,
    string BasicUsername,
    string BasicPassword,
    string BearerToken,
    string OAuthAuthority,
    string OAuthClientId,
    string OAuthClientSecret,
    IDictionary<string, string> Headers,
    bool IsReplayable,
    bool IsActive)
{
    /// <summary>
    /// Handles the command by registering the assembled definition.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> that decides what changed.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(WebhookRegistrar registrar) =>
        registrar.Add(EventStore, [Definition()]);

    /// <summary>
    /// Builds the target the webhook is called through.
    /// </summary>
    /// <returns>The target.</returns>
    internal WebhookTarget Target() =>
        new()
        {
            Url = Url,
            Authorization = Authorization(),
            Headers = Headers.ToDictionary(_ => _.Key, _ => _.Value)
        };

    WebhookDefinition Definition() =>
        new()
        {
            EventSequenceId = EventSequenceId,
            Identifier = Name,
            EventTypes = [.. EventTypes],
            Target = Target(),
            IsReplayable = IsReplayable,
            IsActive = IsActive
        };

    Contracts.Primitives.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? Authorization() =>
        AuthorizationType switch
        {
            AuthorizationType.Basic => new(new BasicAuthorization { Username = BasicUsername, Password = BasicPassword }),
            AuthorizationType.Bearer => new(new BearerTokenAuthorization { Token = BearerToken }),
            AuthorizationType.OAuth => new(new OAuthAuthorization { Authority = OAuthAuthority, ClientId = OAuthClientId, ClientSecret = OAuthClientSecret }),
            _ => null
        };
}
