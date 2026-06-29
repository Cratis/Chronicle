// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Storage.Patching;

namespace Cratis.Chronicle.Storage.InMemory.Patching;

/// <summary>
/// Represents an in-memory implementation of <see cref="IPatchStorage"/>.
/// </summary>
public sealed class PatchStorage : IPatchStorage
{
    readonly ConcurrentDictionary<string, Patch> _patches = new();

    /// <inheritdoc/>
    public Task<IEnumerable<Patch>> GetAll() => Task.FromResult<IEnumerable<Patch>>([.. _patches.Values]);

    /// <inheritdoc/>
    public Task Save(Patch patch)
    {
        _patches[patch.Name] = patch;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> Has(string patchName) => Task.FromResult(_patches.ContainsKey(patchName));

    /// <inheritdoc/>
    public Task Remove(string patchName)
    {
        _patches.TryRemove(patchName, out _);
        return Task.CompletedTask;
    }
}
