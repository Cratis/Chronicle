// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents a user in the Chronicle system.
/// </summary>
[ProtoContract]
public record User
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid Id { get; set; } = Guid.Empty;

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
    /// Whether the user account is active.
    /// </summary>
    [ProtoMember(4)]
    public bool IsActive { get; set; }

    /// <summary>
    /// When the user was created.
    /// </summary>
    [ProtoMember(5)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the user was last modified.
    /// </summary>
    [ProtoMember(6)]
    public DateTimeOffset? LastModifiedAt { get; set; }
}
