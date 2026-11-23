// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents monotonic provenance for one in-memory storage scope.
/// </summary>
public class InMemorySinkProvenanceStorage : ISinkProvenanceStorage
{
    int _quarantined;

    /// <inheritdoc/>
    public Task<bool> IsQuarantined() => Task.FromResult(Volatile.Read(ref _quarantined) != 0);

    /// <inheritdoc/>
    public Task MarkQuarantined()
    {
        Interlocked.Exchange(ref _quarantined, 1);
        return Task.CompletedTask;
    }
}
