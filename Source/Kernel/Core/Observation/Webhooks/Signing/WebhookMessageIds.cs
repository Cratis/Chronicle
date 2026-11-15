// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Computes the deterministic <c language="text">webhook-id</c> of a webhook message.
/// </summary>
internal static class WebhookMessageIds
{
    const string DomainTag = "cratis.webhook-id.v1";
    const int StackLimit = 512;
    static readonly Encoding _utf8 = new UTF8Encoding(false, true);

    /// <summary>
    /// Computes the message id for an identity.
    /// </summary>
    /// <remarks>
    /// The v1 encoding is fixed by this contract version. SHA-256 is taken over: the ASCII domain tag
    /// <c language="text">cratis.webhook-id.v1</c>; then event store, namespace, observer id, event sequence id and kind, each as a
    /// UInt32 big-endian byte length followed by UTF-8 bytes; then the UInt32 big-endian batch count; then per batch
    /// the partition as length-prefixed UTF-8 followed by the first and last sequence numbers as UInt64 big-endian;
    /// last the epoch, byte 0x00 when none, or 0x01 followed by the 16 bytes of the GUID in big-endian order.
    /// The id is <c language="text">msg_</c> followed by the lowercase hex of the hash, 68 characters, and never contains a dot.
    /// </remarks>
    /// <param name="identity">The <see cref="WebhookMessageIdentity"/>.</param>
    /// <returns>The message id.</returns>
    public static string For(WebhookMessageIdentity identity)
    {
        var size = DomainTag.Length;
        size += Sized(identity.EventStore.Value) + Sized(identity.Namespace.Value) + Sized(identity.ObserverId.Value)
            + Sized(identity.EventSequenceId.Value) + Sized(identity.Kind) + sizeof(uint);
        var partitions = new string[identity.Batches.Count];
        for (var i = 0; i < partitions.Length; i++)
        {
            partitions[i] = identity.Batches[i].Partition.ToString() ?? string.Empty;
            size += Sized(partitions[i]) + (2 * sizeof(ulong));
        }
        size += 1 + 16;

        var rented = size > StackLimit ? ArrayPool<byte>.Shared.Rent(size) : null;
        var buffer = rented is null ? stackalloc byte[size] : rented.AsSpan(0, size);
        try
        {
            var position = Encoding.ASCII.GetBytes(DomainTag, buffer);
            position += Write(buffer[position..], identity.EventStore.Value);
            position += Write(buffer[position..], identity.Namespace.Value);
            position += Write(buffer[position..], identity.ObserverId.Value);
            position += Write(buffer[position..], identity.EventSequenceId.Value);
            position += Write(buffer[position..], identity.Kind);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[position..], (uint)partitions.Length);
            position += sizeof(uint);
            for (var i = 0; i < partitions.Length; i++)
            {
                position += Write(buffer[position..], partitions[i]);
                BinaryPrimitives.WriteUInt64BigEndian(buffer[position..], identity.Batches[i].First.Value);
                BinaryPrimitives.WriteUInt64BigEndian(buffer[(position + sizeof(ulong))..], identity.Batches[i].Last.Value);
                position += 2 * sizeof(ulong);
            }

            if (identity.ReplayEpoch is { } epoch)
            {
                buffer[position++] = 1;
                epoch.Value.TryWriteBytes(buffer[position..], bigEndian: true, out var written);
                position += written;
            }
            else
            {
                buffer[position++] = 0;
            }

            return "msg_" + Convert.ToHexStringLower(SHA256.HashData(buffer[..position]));
        }
        finally
        {
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
        }
    }

    static int Sized(string value) => sizeof(uint) + _utf8.GetByteCount(value);

    static int Write(Span<byte> destination, string value)
    {
        var length = _utf8.GetBytes(value, destination[sizeof(uint)..]);
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)length);
        return sizeof(uint) + length;
    }
}
