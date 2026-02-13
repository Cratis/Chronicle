// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for requiring a user to change their password.
/// </summary>
[ProtoContract]
public class RequirePasswordChange
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid UserId { get; set; } = Guid.Empty;
}
