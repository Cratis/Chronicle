// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Keeps one immutable encoding key per in-memory event store with atomic insert-if-absent semantics.
/// </summary>
/// <remarks>
/// This storage is not crash-durable. Future marker admission must be checked within the creation lock,
/// never through a separate read check. This key-only implementation is not wired to live provisioning.
/// </remarks>
public sealed class UniqueConstraintEncodingStorage : IUniqueConstraintEncodingStorage
{
    readonly Lock _gate = new();
    UniqueConstraintEncodingKey? _key;

    /// <inheritdoc/>
    public Task<UniqueConstraintEncodingKey?> GetKey()
    {
        lock (_gate)
        {
            return Task.FromResult(_key);
        }
    }

    /// <inheritdoc/>
    public Task<UniqueConstraintEncodingKey> CreateKeyIfAbsent(UniqueConstraintEncodingKey candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        lock (_gate)
        {
            _key ??= candidate;
            return Task.FromResult(_key);
        }
    }
}
