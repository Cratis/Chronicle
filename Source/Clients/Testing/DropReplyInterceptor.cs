// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Discards selected successful replies after the real mutation, simulating an unknown commit outcome.
/// The caller must inspect or retry using the same identity, just as with a remote kernel.
/// </summary>
public sealed class DropReplyInterceptor
{
    readonly object _sync = new();
    readonly Dictionary<DroppedReply, int> _pending = [];
    int _dropped;

    /// <summary>
    /// Gets how many replies have actually been discarded.
    /// </summary>
    public int Dropped
    {
        get
        {
            lock (_sync) return _dropped;
        }
    }

    /// <summary>
    /// Queues reply loss for the next successful mutations of this kind. Zero queues nothing.
    /// </summary>
    /// <param name="reply">The mutation to intercept.</param>
    /// <param name="count">The number of replies to discard.</param>
    public void DropNext(DroppedReply reply, int count = 1)
    {
        lock (_sync) _pending[reply] = _pending.GetValueOrDefault(reply) + Math.Max(0, count);
    }

    /// <summary>
    /// Discards a selected reply only after the caller confirms a successful mutation.
    /// </summary>
    /// <typeparam name="T">The reply type.</typeparam>
    /// <param name="reply">The mutation kind.</param>
    /// <param name="result">The real result.</param>
    /// <param name="committed">Whether this call settled successfully.</param>
    /// <returns>The unchanged result when not discarded.</returns>
    /// <exception cref="RpcException">The selected reply was discarded after commit.</exception>
    internal T After<T>(DroppedReply reply, T result, bool committed)
    {
        lock (_sync)
        {
            if (!committed || _pending.GetValueOrDefault(reply) == 0) return result;
            _pending[reply]--;
            _dropped++;
        }
        throw new RpcException(new Status(StatusCode.Unavailable, "The scenario discarded the reply after commit."));
    }
}
