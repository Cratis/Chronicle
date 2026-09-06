// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an <see cref="ICurrentPrincipalAccessor"/> for an in-process scenario, where there is no real HTTP
/// request to resolve a principal from.
/// </summary>
/// <remarks>
/// Kernel-side artifacts that fall back to <c>ICurrentPrincipalAccessor.Current</c> when a caller does not supply
/// an explicit identity need this resolvable even outside a real ASP.NET Core pipeline.
/// </remarks>
internal sealed class InProcessCurrentPrincipalAccessor : ICurrentPrincipalAccessor
{
    /// <inheritdoc/>
    public ClaimsPrincipal Current { get; } = new(new ClaimsIdentity());
}
