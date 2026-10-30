// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Describes the authority for an administrator privilege grant.
/// </summary>
public enum GrantReason
{
    /// <summary>
    /// The user was created by initial bootstrap.
    /// </summary>
    Bootstrap = 0,

    /// <summary>
    /// Existing bootstrap provenance was verified during upgrade.
    /// </summary>
    UpgradeProvenance = 1,

    /// <summary>
    /// An operator requested audited, server-local recovery.
    /// </summary>
    OperatorRecovery = 2,

    /// <summary>
    /// An authenticated administrator granted the privilege through the management API.
    /// </summary>
    AdministratorGrant = 3
}
