// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Chronicle.Security;

/// <summary>The local silo's immutable view of explicit grants.</summary>
public interface IStoreEntitlementsCache
{
    /// <summary>Gets the current snapshot.</summary>
    StoreEntitlementsSnapshot Snapshot { get; }

    /// <summary>Gets whether an authoritative snapshot was confirmed within the lease.</summary>
    bool IsFresh { get; }

    /// <summary>Gets the time of the last successful authoritative confirmation.</summary>
    DateTimeOffset? LoadedAt { get; }

    /// <summary>Gets notifications after snapshots change.</summary>
    IObservable<Unit> Changes { get; }

    /// <summary>Loads the cache and starts background version polling.</summary>
    /// <returns>An awaitable task.</returns>
    Task Load();

    /// <summary>Publishes an acknowledged authoritative state on this silo.</summary>
    /// <param name="state">The authoritative state.</param>
    void Publish(StoreEntitlementsState state);
}
