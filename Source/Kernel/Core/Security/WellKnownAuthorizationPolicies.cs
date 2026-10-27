// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Names the administrator authorization policies.
/// </summary>
public static class WellKnownAuthorizationPolicies
{
    /// <summary>
    /// Authorizes the administrator management surface.
    /// </summary>
    public const string Administrator = "Chronicle.Administrator";

    /// <summary>
    /// Authorizes System-store operational recovery, in audit mode by default.
    /// </summary>
    public const string AdministratorOperations = "Chronicle.Administrator.Operations";
}
