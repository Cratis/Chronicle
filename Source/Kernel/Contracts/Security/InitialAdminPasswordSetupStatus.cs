// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the response for checking if initial admin password setup is required.
/// </summary>
[ProtoContract]
public class InitialAdminPasswordSetupStatus
{
    /// <summary>
    /// Gets or sets whether initial admin password setup is required.
    /// </summary>
    [ProtoMember(1)]
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID if setup is required.
    /// </summary>
    [ProtoMember(2)]
    public Guid? AdminUserId { get; set; }
}
