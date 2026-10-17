// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Checks append admission independently of read protection and the ambient unit manager.
/// </summary>
internal static class ImmediateAppendGuard
{
    /// <summary>
    /// Refuses a public immediate append before resolving any of its inputs.
    /// </summary>
    /// <param name="operation">The public append operation.</param>
    /// <exception cref="ImmediateAppendRefused">The current flow refuses immediate appends.</exception>
    internal static void Append(string operation)
    {
        if (ImmediateAppendRefusal.Current is not null) throw new ImmediateAppendRefused(operation);
    }

    /// <summary>
    /// Prevents the flow from replacing or completing a different unit.
    /// </summary>
    /// <param name="unit">The receiving unit.</param>
    /// <exception cref="ImmediateAppendRefusalRequiresOwner">The receiving unit does not own this flow.</exception>
    internal static void RequireUnit(IUnitOfWork unit)
    {
        if (ImmediateAppendRefusal.Current is { } frame && !ReferenceEquals(frame.UnitOfWork, unit)) throw new ImmediateAppendRefusalRequiresOwner();
    }
}
