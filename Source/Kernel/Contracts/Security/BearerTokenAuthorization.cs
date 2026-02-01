// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents bearer token authentication for a webhook.
/// </summary>
[ProtoContract]
public class BearerTokenAuthorization
{
    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    [ProtoMember(1)]
    public string Token { get; set; } = string.Empty;
}
