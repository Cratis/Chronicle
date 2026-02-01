// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents OAuth authentication for a webhook.
/// </summary>
[ProtoContract]
public class OAuthAuthorization
{
    /// <summary>
    /// Gets or sets the OAuth authority.
    /// </summary>
    [ProtoMember(1)]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    [ProtoMember(2)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    [ProtoMember(3)]
    public string ClientSecret { get; set; } = string.Empty;
}
