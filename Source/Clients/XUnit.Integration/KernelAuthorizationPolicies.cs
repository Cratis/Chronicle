// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Arc.Authorization;
using KernelCore::Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Opts the embedded kernel's administrator policies into anonymous evaluation in the shared in-process host.
/// </summary>
/// <remarks>
/// The kernel owns the policies and their handlers, registered when it is added to the silo. They are requirement-only,
/// so the handlers alone decide: with the default configuration an anonymous caller is denied the administrator policy and
/// the operations policy runs in audit mode. This only lets Arc evaluate them without an authenticated caller. An application
/// policy of the same name without the kernel's requirement is rejected with <see cref="KernelAuthorizationPolicyNameIsReserved"/>.
/// </remarks>
static class KernelAuthorizationPolicies
{
    /// <summary>
    /// Opts the administrator policies into anonymous evaluation.
    /// </summary>
    /// <param name="services">The shared host's services.</param>
    internal static void Apply(IServiceCollection services)
    {
        services.AddArcAnonymousAspNetAuthorizationPolicy(WellKnownAuthorizationPolicies.Administrator);
        services.AddArcAnonymousAspNetAuthorizationPolicy(WellKnownAuthorizationPolicies.AdministratorOperations);
    }
}
