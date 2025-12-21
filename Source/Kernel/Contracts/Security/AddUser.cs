// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for adding a user.
/// </summary>
[ProtoContract]
public record AddUser
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [ProtoMember(1)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The username.
    /// </summary>
    [ProtoMember(2)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    [ProtoMember(3)]
    public string? Email { get; set; }

    /// <summary>
    /// The password.
    /// </summary>
    [ProtoMember(4)]
    public string Password { get; set; } = string.Empty;
}
