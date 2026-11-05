// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services;

/// <summary>
/// The routing values a query addresses: the event store and up to five further components that form part of a grain or storage key.
/// </summary>
/// <param name="Store">The event store name, or null when the query does not address one.</param>
/// <param name="First">The first further routing component.</param>
/// <param name="Second">The second further routing component.</param>
/// <param name="Third">The third further routing component.</param>
/// <param name="Fourth">The fourth further routing component.</param>
/// <param name="Fifth">The fifth further routing component.</param>
/// <param name="Capability">The capability the caller is decided for at the addressed event store, or null when the query needs none.</param>
internal readonly record struct QueryRouting(string? Store, string? First = null, string? Second = null, string? Third = null, string? Fourth = null, string? Fifth = null, string? Capability = null)
{
    /// <summary>
    /// Gets a value indicating whether the query addresses the System event store.
    /// </summary>
    internal bool TargetsSystem => SystemStoreAccess.IsSystem(Store);

    /// <summary>
    /// Gets a value indicating whether any routing value contains the grain key separator.
    /// </summary>
    internal bool HasSeparator => SystemStoreAccess.FirstSeparatorViolation(Store, First, Second, Third, Fourth, Fifth) >= 0;
}
