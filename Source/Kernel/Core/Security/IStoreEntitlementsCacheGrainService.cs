// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;
using Orleans.Services;

namespace Cratis.Chronicle.Security;

/// <summary>Refreshes the local silo's grant snapshot.</summary>
[Version(1)]
public interface IStoreEntitlementsCacheGrainService : IGrainService
{
    /// <summary>Publishes a committed authoritative state.</summary>
    /// <param name="state">The committed state.</param>
    /// <returns>An awaitable task.</returns>
    Task Refresh(StoreEntitlementsState state);
}
