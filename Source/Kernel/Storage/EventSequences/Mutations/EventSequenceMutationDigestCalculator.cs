// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Calculates provider-neutral version 1 digests for event sequence mutation definitions and terminal receipts.
/// </summary>
public static class EventSequenceMutationDigestCalculator
{
    static readonly byte[] _idDomain = Encoding.ASCII.GetBytes("cratis.chronicle/event-sequence-mutation-id");
    static readonly byte[] _definitionDomain = Encoding.ASCII.GetBytes("cratis.chronicle.event-sequence-mutation.definition");
    static readonly byte[] _receiptDomain = Encoding.ASCII.GetBytes("cratis.chronicle.event-sequence-mutation.terminal-receipt");

    /// <summary>
    /// Calculates the deterministic version 1 identifier for a mutation origin and target event sequence.
    /// </summary>
    /// <param name="target">The target event sequence identity.</param>
    /// <param name="origin">The originating event sequence identity.</param>
    /// <param name="originSequenceNumber">The sequence number of the event that originated the mutation.</param>
    /// <param name="kind">The kind of mutation.</param>
    /// <returns>The deterministic mutation identifier.</returns>
    public static EventSequenceMutationId CalculateId(
        EventSequenceMutationIdentity target,
        EventSequenceMutationIdentity origin,
        EventSequenceNumber originSequenceNumber,
        EventSequenceMutationKind kind)
    {
        var frame = new CanonicalFrameWriter(_idDomain, 1);
        frame.WriteText("targetSequence", target);
        frame.WriteText("originSequence", origin);
        frame.WriteUInt64(originSequenceNumber.Value);
        frame.WriteInt32((int)kind);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(frame.WrittenSpan, hash);
        var idBytes = hash[..16];
        idBytes[6] = (byte)((idBytes[6] & 0x0f) | 0x80);
        idBytes[8] = (byte)((idBytes[8] & 0x3f) | 0x80);

        return new(new Guid(idBytes, bigEndian: true));
    }

    /// <summary>
    /// Calculates the version 1 definition digest for a mutation in an event sequence scope.
    /// </summary>
    /// <param name="scope">The target event sequence scope.</param>
    /// <param name="mutation">The mutation definition.</param>
    /// <returns>The version 1 definition digest.</returns>
    /// <exception cref="UnsupportedEventSequenceId">Thrown when the target or origin event sequence identifier is unsupported.</exception>
    public static EventSequenceMutationDefinitionDigestV1 CalculateDefinitionDigest(EventSequenceKey scope, EventSequenceMutation mutation)
    {
        var targetIdentity = GetIdentity(scope.EventSequenceId.Value);
        var originIdentity = GetIdentity(mutation.Origin.Sequence.Value);
        var frame = new CanonicalFrameWriter(_definitionDomain);
        frame.WriteText("eventStore", scope.EventStore.Value);
        frame.WriteText("namespace", scope.Namespace.Value);
        frame.WriteText("targetSequence", targetIdentity);
        frame.WriteGuid(mutation.Id.Value);
        frame.WriteText("originSequence", originIdentity);
        frame.WriteUInt64(mutation.Origin.SequenceNumber.Value);
        frame.WriteInt32((int)mutation.Command.Kind);
        frame.WriteText("commandPayload", mutation.Command.Payload);
        frame.WriteText("commandHash", mutation.Command.Hash.Value);
        frame.WriteUInt64(mutation.Target.Start.Value);
        frame.WriteUInt64(mutation.Target.EndExclusive.Value);
        frame.WriteUInt64(mutation.Target.ExpectedCount.Value);

        return new(SHA256.HashData(frame.WrittenSpan));
    }

    /// <summary>
    /// Calculates the version 1 terminal receipt digest for a mutation in an event sequence scope.
    /// </summary>
    /// <param name="scope">The target event sequence scope.</param>
    /// <param name="receipt">The persisted terminal receipt fields.</param>
    /// <param name="finalStateVersion">The final persisted mutation state version.</param>
    /// <param name="definitionDigest">The version 1 mutation definition digest.</param>
    /// <returns>The version 1 terminal receipt digest.</returns>
    /// <exception cref="UnsupportedEventSequenceId">Thrown when the target or origin event sequence identifier is unsupported.</exception>
    public static EventSequenceMutationReceiptDigestV1 CalculateReceiptDigest(
        EventSequenceKey scope,
        EventSequenceMutationHistoryEntry receipt,
        EventSequenceMutationStateVersion finalStateVersion,
        EventSequenceMutationDefinitionDigestV1 definitionDigest)
    {
        var targetIdentity = GetIdentity(scope.EventSequenceId.Value);
        var originIdentity = GetIdentity(receipt.Origin.Sequence.Value);
        var frame = new CanonicalFrameWriter(_receiptDomain);
        frame.WriteText("eventStore", scope.EventStore.Value);
        frame.WriteText("namespace", scope.Namespace.Value);
        frame.WriteText("targetSequence", targetIdentity);
        frame.WriteGuid(receipt.Id.Value);
        frame.WriteInt64(receipt.Ordinal.Value);
        frame.WriteText("originSequence", originIdentity);
        frame.WriteUInt64(receipt.Origin.SequenceNumber.Value);
        frame.WriteInt32((int)receipt.Kind);
        frame.WriteText("commandHash", receipt.CommandHash.Value);
        frame.WriteUInt64(receipt.Target.Start.Value);
        frame.WriteUInt64(receipt.Target.EndExclusive.Value);
        frame.WriteUInt64(receipt.Target.ExpectedCount.Value);
        frame.WriteInt32((int)receipt.RepairState);
        frame.WriteInt64(finalStateVersion.Value);
        frame.WriteRaw(definitionDigest.Snapshot());

        return new(SHA256.HashData(frame.WrittenSpan));
    }

    static EventSequenceMutationIdentity GetIdentity(string display)
    {
        var result = EventSequenceMutationIdentity.TryCreate(display);
        if (!result.IsSuccess)
        {
            throw new UnsupportedEventSequenceId(display, result.Reason!.Value);
        }

        return result.Identity!;
    }

    sealed class CanonicalFrameWriter
    {
        static readonly UTF8Encoding _strictUtf8 = new(false, true);
        readonly ArrayBufferWriter<byte> _buffer = new();

        internal CanonicalFrameWriter(ReadOnlySpan<byte> domain)
        {
            _buffer.Write(domain);
            WriteByte(0);
            WriteByte(1);
        }

        internal CanonicalFrameWriter(ReadOnlySpan<byte> domain, ushort version)
        {
            _buffer.Write(domain);
            WriteByte(0);
            WriteUInt16(version);
        }

        internal ReadOnlySpan<byte> WrittenSpan => _buffer.WrittenSpan;

        internal void WriteText(string field, string? value)
        {
            byte[] bytes;
            try
            {
                bytes = _strictUtf8.GetBytes(value ?? throw new InvalidEventSequenceMutationFrameText(field));
            }
            catch (EncoderFallbackException)
            {
                throw new InvalidEventSequenceMutationFrameText(field);
            }

            Span<byte> length = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)bytes.Length));
            _buffer.Write(length);
            _buffer.Write(bytes);
        }

        internal void WriteText(string field, EventSequenceMutationIdentity identity)
        {
            if (!identity.Key.IsInitialized)
            {
                throw new InvalidEventSequenceMutationFrameText(field);
            }

            var bytes = identity.Key.Snapshot();
            Span<byte> length = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)bytes.Length));
            _buffer.Write(length);
            _buffer.Write(bytes);
        }

        internal void WriteGuid(Guid value)
        {
            Span<byte> bytes = stackalloc byte[16];
            value.TryWriteBytes(bytes, bigEndian: true, out _);
            _buffer.Write(bytes);
        }

        internal void WriteUInt16(ushort value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            _buffer.Write(bytes);
        }

        internal void WriteInt32(int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            _buffer.Write(bytes);
        }

        internal void WriteInt64(long value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            _buffer.Write(bytes);
        }

        internal void WriteUInt64(ulong value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            _buffer.Write(bytes);
        }

        internal void WriteRaw(ReadOnlySpan<byte> bytes) => _buffer.Write(bytes);

        void WriteByte(byte value)
        {
            var span = _buffer.GetSpan(1);
            span[0] = value;
            _buffer.Advance(1);
        }
    }
}
