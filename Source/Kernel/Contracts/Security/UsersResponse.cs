// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the response containing users.
/// </summary>
[ProtoContract]
public class UsersResponse
{
    /// <summary>
    /// Gets or sets the collection of users.
    /// </summary>
    [ProtoMember(1)]
    public IList<User> Users { get; set; } = [];
}
