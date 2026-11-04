// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IResourceNameReservations"/>.
/// </summary>
public class InMemoryResourceNameReservations : ResourceNameReservations
{
    readonly ConcurrentDictionary<string, string> _reservations = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    protected override Task<string?> TryAdd(string key, string owner)
    {
        var existing = _reservations.GetOrAdd(key, owner);
        return Task.FromResult<string?>(ReferenceEquals(existing, owner) ? null : existing);
    }

    /// <inheritdoc/>
    protected override Task Remove(IReadOnlyCollection<string> keys, string owner)
    {
        foreach (var key in keys)
        {
            _reservations.TryRemove(new KeyValuePair<string, string>(key, owner));
        }

        return Task.CompletedTask;
    }
}
