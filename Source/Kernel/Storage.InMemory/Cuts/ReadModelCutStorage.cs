// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Cuts;

namespace Cratis.Chronicle.Storage.InMemory.Cuts;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReadModelCutStorage"/>.
/// </summary>
public class ReadModelCutStorage : IReadModelCutStorage
{
    readonly object _lock = new();
    readonly Dictionary<(Guid Id, string ReadModel), string> _payloads = [];
    readonly Dictionary<Guid, ReadModelCutManifest> _manifests = [];

    /// <inheritdoc/>
    public Task SavePayload(ReadModelCutId id, ReadModelIdentifier readModel, string payloadJson)
    {
        lock (_lock)
        {
            _payloads[(id.Value, readModel.Value)] = payloadJson;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetPayload(ReadModelCutId id, ReadModelIdentifier readModel)
    {
        lock (_lock)
        {
            return Task.FromResult(_payloads.TryGetValue((id.Value, readModel.Value), out var payload) ? payload : null);
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasManifest(ReadModelCutId id)
    {
        lock (_lock)
        {
            return Task.FromResult(_manifests.ContainsKey(id.Value));
        }
    }

    /// <inheritdoc/>
    public Task<ReadModelCutManifest?> GetManifest(ReadModelCutId id)
    {
        lock (_lock)
        {
            return Task.FromResult(_manifests.TryGetValue(id.Value, out var manifest) ? manifest : null);
        }
    }

    /// <inheritdoc/>
    public Task PublishManifest(ReadModelCutManifest manifest)
    {
        lock (_lock)
        {
            _manifests.TryAdd(manifest.Id.Value, manifest);
        }

        return Task.CompletedTask;
    }
}
