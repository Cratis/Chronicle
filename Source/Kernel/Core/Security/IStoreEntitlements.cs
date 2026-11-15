// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Security;

/// <summary>Serializes changes to explicit event store grants.</summary>
[Version(1)]
public interface IStoreEntitlements : IGrainWithStringKey
{
    /// <summary>The singleton grain key.</summary>
    public const string Key = "store-entitlements";

    /// <summary>Creates or replaces a grant.</summary>
    /// <param name="grant">The explicit grant.</param>
    /// <returns>An awaitable task.</returns>
    Task Grant(EventStoreAccessGrant grant);

    /// <summary>Removes a grant by its identity.</summary>
    /// <param name="grant">The grant identity.</param>
    /// <returns>Whether a grant was removed.</returns>
    Task<bool> Revoke(EventStoreAccessGrant grant);

    /// <summary>Reconciles configuration without changing command-sourced grants.</summary>
    /// <param name="grants">The configured grants.</param>
    /// <returns>An awaitable task.</returns>
    Task Reconcile(IEnumerable<ConfiguredGrant> grants);

    /// <summary>Gets the authoritative grants.</summary>
    /// <returns>The current snapshot.</returns>
    Task<StoreEntitlementsState> GetSnapshot();

    /// <summary>Gets the current version.</summary>
    /// <returns>The current version.</returns>
    Task<long> GetVersion();
}
