// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics;

/// <summary>
/// Tracks storage authorization failures for the lifetime of the process.
/// </summary>
/// <param name="logger">The diagnostic logger.</param>
[Singleton]
public class StorageAuthorizationDiagnostics(ILogger<StorageAuthorizationDiagnostics> logger) : IStorageAuthorizationDiagnostics
{
    readonly ConcurrentDictionary<StorageAuthorizationFailure, byte> _failures = new();

    /// <inheritdoc/>
    public IReadOnlyCollection<StorageAuthorizationFailure> Failures => _failures.Keys.ToArray();

    /// <inheritdoc/>
    public bool Record(StorageAuthorizationFailure failure)
    {
        if (!_failures.TryAdd(failure, 0)) return false;
        logger.StorageRefused(failure.Operation, failure.Collection, failure.Database);
        return true;
    }

    /// <inheritdoc/>
    public void Clear(string database, string collection)
    {
        if (_failures.IsEmpty) return;
        foreach (var failure in _failures.Keys.Where(_ => _.Database == database && _.Collection == collection))
        {
            if (_failures.TryRemove(failure, out _)) logger.StorageRecovered(collection, database);
        }
    }
}
