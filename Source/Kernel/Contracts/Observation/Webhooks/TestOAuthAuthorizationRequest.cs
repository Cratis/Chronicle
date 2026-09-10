// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the request for testing OAuth authorization.
/// </summary>
[ProtoContract]
public class TestOAuthAuthorizationRequest
{
    /// <summary>
    /// Gets or sets the OAuth authority URL.
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
