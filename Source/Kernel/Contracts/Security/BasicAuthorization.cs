// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents basic authentication for a webhook.
/// </summary>
[ProtoContract]
public class BasicAuthorization
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [ProtoMember(1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [ProtoMember(2)]
    public string Password { get; set; } = string.Empty;
}
