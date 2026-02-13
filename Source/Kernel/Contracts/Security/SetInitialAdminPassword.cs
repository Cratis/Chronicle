// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for setting the initial admin password.
/// </summary>
[ProtoContract]
public class SetInitialAdminPassword
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

    /// <summary>
    /// The confirmed new password.
    /// </summary>
    [ProtoMember(3)]
    public string ConfirmedPassword { get; set; } = string.Empty;
}
