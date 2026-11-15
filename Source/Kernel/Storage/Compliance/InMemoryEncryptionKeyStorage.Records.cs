// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

public partial class InMemoryEncryptionKeyStorage : IEncryptionKeyFenceRecords
{
    readonly Dictionary<(EventStoreName, EventStoreNamespaceName, string), string> _records = [];
    readonly Dictionary<(Scope, string), EncryptionKeyErasureRequest> _archivedRequests = [];

    /// <inheritdoc/>
    public Task<long?> CountLegacyFences(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) => Task.FromResult<long?>(0);

    /// <inheritdoc/>
    public Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_records.GetValueOrDefault((eventStore, eventStoreNamespace, id)));
        }
    }

    /// <inheritdoc/>
    public Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false)
    {
        lock (_lock)
        {
            if (replace) _records[(eventStore, eventStoreNamespace, id)] = value;
            else _records.TryAdd((eventStore, eventStoreNamespace, id), value);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            return Task.FromResult(EnsureFence(scope).Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? _archivedRequests.GetValueOrDefault((scope, requestId)));
        }
    }

    /// <inheritdoc/>
    public Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retain);
        lock (_lock)
        {
            var scope = new Scope(eventStore, eventStoreNamespace, identifier);
            var fence = EnsureFence(scope);
            var removable = (fence.Requests ?? []).Where(_ => _.Confirmed).Reverse().Skip(retain).ToArray();
            foreach (var request in removable) _archivedRequests.TryAdd((scope, request.RequestId), request);
            var removed = removable.Select(_ => _.RequestId).ToHashSet(StringComparer.Ordinal);
            _erasures[scope] = fence with { Version = checked(fence.Version + 1), Requests = (fence.Requests ?? []).Where(_ => !removed.Contains(_.RequestId)).ToArray() };
        }

        return Task.CompletedTask;
    }
}
