// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for removing a user.
/// </summary>
[ProtoContract]
public class RemoveUser
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid UserId { get; set; } = Guid.Empty;
}
