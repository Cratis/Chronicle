// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Computes the canonical identities specified by decision 0020.
/// </summary>
public static class DeliveryIdentity
{
    /// <summary>
    /// Computes an event key that survives rebatching.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="observer">The observer.</param>
    /// <param name="sequence">The event sequence.</param>
    /// <param name="number">The event sequence number.</param>
    /// <returns>The lowercase SHA-256 key.</returns>
    public static EventDedupKey Event(EventStoreName store, EventStoreNamespaceName @namespace, ObserverId observer, EventSequenceId sequence, EventSequenceNumber number)
    {
        using var bytes = Prefix("cratis.event.v1", store, @namespace, observer, sequence);
        WriteNumber(bytes, number.Value);
        return new(Hash(bytes));
    }

    /// <summary>
    /// Computes a delivery identifier without attempt-specific data.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="observer">The observer.</param>
    /// <param name="sequence">The event sequence.</param>
    /// <param name="kind">The envelope kind.</param>
    /// <param name="ranges">Partition ranges in envelope order; empty for control envelopes.</param>
    /// <returns>The prefixed lowercase SHA-256 identifier.</returns>
    public static DeliveryId Delivery(EventStoreName store, EventStoreNamespaceName @namespace, ObserverId observer, EventSequenceId sequence, DeliveryKind kind, IEnumerable<(string Partition, ulong First, ulong Last)> ranges)
    {
        using var bytes = Prefix("cratis.delivery.v1", store, @namespace, observer, sequence);
        WriteString(bytes, KindName(kind));
        foreach (var range in ranges)
        {
            WriteString(bytes, range.Partition);
            WriteNumber(bytes, range.First);
            WriteNumber(bytes, range.Last);
        }

        return new($"msg_{Hash(bytes)}");
    }

    static string KindName(DeliveryKind kind) => kind switch
    {
        DeliveryKind.Events => "events",
        DeliveryKind.Ping => "ping",
        DeliveryKind.ReplayStarted => "replay-started",
        DeliveryKind.ReplayEnded => "replay-ended",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    static MemoryStream Prefix(string domain, EventStoreName store, EventStoreNamespaceName @namespace, ObserverId observer, EventSequenceId sequence)
    {
        var bytes = new MemoryStream();
        WriteString(bytes, domain);
        WriteString(bytes, store.Value);
        WriteString(bytes, @namespace.Value);
        WriteString(bytes, observer.Value);
        WriteString(bytes, sequence.Value);
        return bytes;
    }

    static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)bytes.Length));
        stream.Write(length);
        stream.Write(bytes);
    }

    static void WriteNumber(Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    static string Hash(MemoryStream bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length))));
}
