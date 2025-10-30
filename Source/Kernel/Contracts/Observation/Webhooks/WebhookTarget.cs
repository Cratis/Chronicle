// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
[ProtoContract]
public class WebhookTarget
{
    /// <summary>
    /// Gets or sets the URL to send the events to.
    /// </summary>
    [ProtoMember(1)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the basic authorization.
    /// </summary>
    [ProtoMember(2)]
    public BasicAuthorization? BasicAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the bearer token authorization.
    /// </summary>
    [ProtoMember(3)]
    public BearerTokenAuthorization? BearerTokenAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the OAuth authorization.
    /// </summary>
    [ProtoMember(4)]
    public OAuthAuthorization? OAuthAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
