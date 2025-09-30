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
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets <see cref="AuthenticationType"/> to use.
    /// </summary>
    [ProtoMember(2)]
    public AuthenticationType Authentication { get; set; } = AuthenticationType.None;

    /// <summary>
    /// Gets or sets optional the username.
    /// </summary>
    [ProtoMember(3)]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets optional the password.
    /// </summary>
    [ProtoMember(4)]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the optional bearer token.
    /// </summary>
    [ProtoMember(5)]
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}