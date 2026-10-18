// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Tracks deliveries for one observation stream, including abandoned waits awaiting a late answer.
/// </summary>
/// <typeparam name="TResult">The subscriber result type.</typeparam>
internal sealed class PendingDeliveries<TResult>
{
    readonly Lock _lock = new();
    readonly Dictionary<ulong, Pending> _byId = [];
    readonly Dictionary<EventSourceId, LinkedList<ulong>> _byPartition = [];
    ulong _lastId;
    bool _disconnected;
    TResult _disconnectedResult = default!;

    /// <summary>
    /// Registers a delivery before sending it to the client.
    /// </summary>
    /// <param name="partition">The partition being delivered.</param>
    /// <param name="completion">The wait to complete with the client's result.</param>
    /// <returns>A nonzero stream-local delivery identifier.</returns>
    public ulong Add(EventSourceId partition, TaskCompletionSource<TResult> completion)
    {
        ulong id;
        lock (_lock)
        {
            id = checked(++_lastId);
            if (!_disconnected)
            {
                if (!_byPartition.TryGetValue(partition, out var ids))
                {
                    ids = new LinkedList<ulong>();
                    _byPartition.Add(partition, ids);
                }
                _byId.Add(id, new Pending(partition, ids.AddLast(id), completion));
                return id;
            }
        }
        completion.TrySetResult(_disconnectedResult);
        return id;
    }

    /// <summary>
    /// Completes exactly the named delivery, or the oldest for an id-less client.
    /// </summary>
    /// <param name="id">The echoed identifier, or zero for a legacy client.</param>
    /// <param name="partition">The partition echoed by the client.</param>
    /// <param name="result">The delivery result.</param>
    /// <returns>Whether a pending wait accepted the result.</returns>
    public bool TryComplete(ulong id, EventSourceId partition, TResult result)
    {
        Pending pending;
        lock (_lock)
        {
            if (id == 0 && _byPartition.TryGetValue(partition, out var ids))
            {
                id = ids.First!.Value;
            }
            if (!_byId.TryGetValue(id, out pending!) || pending.Partition != partition)
            {
                return false;
            }

            _byId.Remove(id);
            var partitionIds = _byPartition[pending.Partition];
            partitionIds.Remove(pending.Node);
            if (partitionIds.Count == 0)
            {
                _byPartition.Remove(pending.Partition);
            }
        }

        // Remove before waking the caller: its continuation can register another delivery immediately.
        return pending.Completion.TrySetResult(result);
    }

    /// <summary>
    /// Fails all pending and subsequently registered deliveries on this stream.
    /// </summary>
    /// <param name="result">The disconnected subscriber result.</param>
    public void Disconnect(TResult result)
    {
        Pending[] pending;
        lock (_lock)
        {
            _disconnected = true;
            _disconnectedResult = result;
            pending = _byId.Values.ToArray();
            _byId.Clear();
            _byPartition.Clear();
        }
        foreach (var delivery in pending)
        {
            delivery.Completion.TrySetResult(result);
        }
    }

    sealed record Pending(EventSourceId Partition, LinkedListNode<ulong> Node, TaskCompletionSource<TResult> Completion);
}
