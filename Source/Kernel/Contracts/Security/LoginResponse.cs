// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the response from a login request.
/// </summary>
[ProtoContract]
public record LoginResponse
{
    /// <summary>
    /// Whether the login was successful.
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    /// Whether the user needs to change their password.
    /// </summary>
    [ProtoMember(2)]
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// The user's ID if login was successful.
    /// </summary>
    [ProtoMember(3)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// An error message if login failed.
    /// </summary>
    [ProtoMember(4)]
    public string? ErrorMessage { get; set; }
}
