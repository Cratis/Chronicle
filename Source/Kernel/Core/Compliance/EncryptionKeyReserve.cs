// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Keeps a small reserve of generated encryption keys, refilled in the background on every core.
/// </summary>
/// <remarks>
/// <para>
/// Generating an RSA key pair costs tens of milliseconds of CPU, and every subject an append introduces needs one.
/// Appends are processed one event at a time, so a batch introducing hundreds of subjects generated their keys one
/// after the other on a single core - the dominant cost of such a batch, enough to take it past a client's deadline.
/// Taking a key from the reserve and refilling it in parallel spreads that work over every core.
/// </para>
/// <para>
/// A reserved key is a fresh, random key pair belonging to nobody until it is taken, exactly like one generated on
/// demand. Every key is handed out once. The reserve fills only after a key has been asked for, so a kernel that never
/// protects a value never generates one.
/// </para>
/// </remarks>
/// <param name="generate">Generates one key pair.</param>
/// <param name="capacity">The number of keys to keep in reserve.</param>
internal sealed class EncryptionKeyReserve(Func<EncryptionKey> generate, int capacity)
{
    readonly ConcurrentQueue<EncryptionKey> _reserve = new();
    int _reservedOrPending;

    /// <summary>
    /// Takes a key from the reserve, or generates one when the reserve is empty, and starts refilling the reserve.
    /// </summary>
    /// <returns>A key pair nobody else has been given.</returns>
    public EncryptionKey Take()
    {
        var key = _reserve.TryDequeue(out var reserved) ? reserved : null;
        if (key is not null)
        {
            Interlocked.Decrement(ref _reservedOrPending);
        }

        Refill();
        return key ?? generate();
    }

    void Refill()
    {
        while (true)
        {
            var current = Volatile.Read(ref _reservedOrPending);
            if (current >= capacity || Interlocked.CompareExchange(ref _reservedOrPending, current + 1, current) != current)
            {
                if (current >= capacity) return;
                continue;
            }

            _ = Task.Run(Generate);
        }
    }

    void Generate()
    {
        try
        {
            _reserve.Enqueue(generate());
        }
        catch
        {
            // Generating on demand reports the failure to whoever asked for a key; the reserve only gives up its slot.
            Interlocked.Decrement(ref _reservedOrPending);
        }
    }
}
