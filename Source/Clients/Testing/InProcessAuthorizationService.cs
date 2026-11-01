// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Allows in-process kernel service calls without a hosting application's authentication setup.
/// </summary>
internal sealed class InProcessAuthorizationService : IAuthorizationService
{
    /// <inheritdoc/>
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
        Task.FromResult(AuthorizationResult.Success());

    /// <inheritdoc/>
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
        Task.FromResult(AuthorizationResult.Success());
}
