// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a command to create a webhook.
/// </summary>
public class AddWebhook
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [FromRoute]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the webhook.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    public string EventSequenceId { get; set; } = "event-log";

    /// <summary>
    /// Gets or sets the event types.
    /// </summary>
    public IEnumerable<Events.EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the authorization type (None, Basic, Bearer, OAuth).
    /// </summary>
    public Security.AuthorizationType AuthorizationType { get; set; } = Security.AuthorizationType.None;

    /// <summary>
    /// Gets or sets the username for Basic authorization.
    /// </summary>
    public string? BasicUsername { get; set; }

    /// <summary>
    /// Gets or sets the password for Basic authorization.
    /// </summary>
    public string? BasicPassword { get; set; }

    /// <summary>
    /// Gets or sets the bearer token for Bearer authorization.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the OAuth authority URL.
    /// </summary>
    public string? OAuthAuthority { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string? OAuthClientSecret { get; set; }

    /// <summary>
    /// Gets or sets additional headers.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets whether the webhook is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
