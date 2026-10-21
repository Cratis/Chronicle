// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when event enrollment supplies a reserved scope origin.
/// </summary>
/// <param name="origin">The reserved origin.</param>
public class ConcurrencyScopeOriginNotValidForEvents(ConcurrencyScopeOrigin origin)
    : Exception($"Scope origin '{origin}' cannot be supplied by event enrollment.")
{
    /// <summary>
    /// Rejects origins reserved for other enrollment paths.
    /// </summary>
    /// <param name="origin">The event-enrollment origin.</param>
    /// <exception cref="ConcurrencyScopeOriginNotValidForEvents">The supplied origin is reserved.</exception>
    internal static void ThrowIfNotValid(ConcurrencyScopeOrigin origin)
    {
        if (origin is not ConcurrencyScopeOrigin.Explicit and not ConcurrencyScopeOrigin.Aggregate and not ConcurrencyScopeOrigin.Routing)
        {
            throw new ConcurrencyScopeOriginNotValidForEvents(origin);
        }
    }
}
