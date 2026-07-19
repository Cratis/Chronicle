// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="IDataProtectionKeyStorage"/>.
/// </summary>
public sealed class DataProtectionKeyStorage : IDataProtectionKeyStorage
{
    readonly ConcurrentBag<DataProtectionKey> _keys = [];

    /// <inheritdoc/>
    public Task<IEnumerable<DataProtectionKey>> GetAll() =>
        Task.FromResult<IEnumerable<DataProtectionKey>>([.. _keys]);

    /// <inheritdoc/>
    public Task Store(DataProtectionKey key)
    {
        _keys.Add(key);
        return Task.CompletedTask;
    }
}
