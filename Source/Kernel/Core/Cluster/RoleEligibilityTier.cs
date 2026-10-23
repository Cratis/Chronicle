// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Describes a candidate silo's eligibility for a role.
/// </summary>
public enum RoleEligibilityTier
{
    /// <summary>
    /// The silo is known to have the role.
    /// </summary>
    HasRole = 0,

    /// <summary>
    /// The silo's roles are not known.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The silo is known not to have the role.
    /// </summary>
    LacksRole = 2
}
