// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
public class WebhookTarget
{
    /// <summary>
    /// Gets or sets the webhook target url.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the basic authorization username.
    /// </summary>
    public string? BasicAuthUsername { get; set; }

    /// <summary>
    /// Gets or sets the basic authorization password.
    /// </summary>
    public string? BasicAuthPassword { get; set; }

    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the OAuth authority.
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
    /// Gets or sets the headers.
    /// </summary>
    [Json]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
