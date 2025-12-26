// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for changing a user's password.
/// </summary>
[ProtoContract]
public record ChangeUserPassword
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid UserId { get; set; } = Guid.Empty;

    /// <summary>
    /// The new password.
    /// </summary>
    [ProtoMember(2)]
    public string Password { get; set; } = string.Empty;
}
