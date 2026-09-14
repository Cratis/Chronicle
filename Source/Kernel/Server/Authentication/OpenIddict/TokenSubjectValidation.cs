// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Validates that token subjects are stable, non-empty Chronicle identifiers.
/// </summary>
internal static class TokenSubjectValidation
{
    /// <summary>
    /// Tries to parse a stable application identifier.
    /// </summary>
    /// <param name="value">The stored OpenIddict application identifier.</param>
    /// <param name="applicationId">The parsed identifier.</param>
    /// <returns>True when the identifier is valid and non-empty.</returns>
    public static bool TryGetApplicationId([NotNullWhen(true)] string? value, out Guid applicationId) =>
        Guid.TryParse(value, out applicationId) && applicationId != Guid.Empty;

    /// <summary>
    /// Gets whether a user identifier is stable and non-empty.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>True when the identifier is valid.</returns>
    public static bool IsStableUserId(UserId userId) => userId != UserId.NotSet;
}
