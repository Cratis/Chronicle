// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Security;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

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
    /// Gets or sets the basic authorization.
    /// </summary>
    public BasicAuthorization? BasicAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the bearer token authorization.
    /// </summary>
    public BearerTokenAuthorization? BearerTokenAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the OAuth authorization.
    /// </summary>
    public OAuthAuthorization? OAuthAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
