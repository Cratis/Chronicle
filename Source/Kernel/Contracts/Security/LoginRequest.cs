// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the request for logging in.
/// </summary>
[ProtoContract]
public record LoginRequest
{
    /// <summary>
    /// The username.
    /// </summary>
    [ProtoMember(1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password.
    /// </summary>
    [ProtoMember(2)]
    public string Password { get; set; } = string.Empty;
}
