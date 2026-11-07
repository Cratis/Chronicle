// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>Broadcasts authoritative grant changes to every active silo.</summary>
public interface IStoreEntitlementsCacheClient
{
    /// <summary>Refreshes each silo without calling back into the mutating grain.</summary>
    /// <param name="state">The committed authoritative state.</param>
    /// <returns>An awaitable task.</returns>
    Task Refresh(StoreEntitlementsState state);
}
