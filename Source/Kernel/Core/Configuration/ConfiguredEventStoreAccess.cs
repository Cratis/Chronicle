// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Configuration;

/// <summary>An explicitly configured ordinary event store scope.</summary>
public class ConfiguredEventStoreAccess
{
    /// <summary>Gets the event store, when not covering all ordinary stores.</summary>
    public string? EventStore { get; init; }

    /// <summary>Gets whether every ordinary event store is covered.</summary>
    public bool AllEventStores { get; init; }

    /// <summary>Gets the namespace, or null for every namespace.</summary>
    public string? Namespace { get; init; }

    /// <summary>Gets the explicit access flags.</summary>
    public StoreAccess Access { get; init; }
}
