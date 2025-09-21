// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

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
    /// Gets or sets the <see cref="AuthenticationType"/>.
    /// </summary>
    public AuthenticationType Authentication { get; set; } = AuthenticationType.None;

    /// <summary>
    /// Gets or sets the optional username.
    /// </summary>
    public string? Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional password.
    /// </summary>
    public string? Passsword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional bearer token.
    /// </summary>
    public string? BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}