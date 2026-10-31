// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents the frozen, bounded canonical bytes of a sequencing key.
/// </summary>
public sealed record CanonicalSequencingKey
{
    /// <summary>
    /// The inclusive canonical key byte limit.
    /// </summary>
    public const int MaximumLength = 8192;

    readonly byte[] _bytes;

    CanonicalSequencingKey(ReadOnlySpan<byte> bytes) => _bytes = bytes.ToArray();

    /// <summary>
    /// Gets the immutable canonical bytes.
    /// </summary>
    public ReadOnlySpan<byte> Span => _bytes;

    /// <summary>
    /// Canonicalizes a logical key, using a digest only for default event source ids or unresolved keys.
    /// </summary>
    /// <param name="key">The logical sequencing key.</param>
    /// <returns>The canonical key.</returns>
    /// <exception cref="InvalidSequencingKey">The key is missing or unsupported.</exception>
    public static CanonicalSequencingKey From(SequencingKey key) => key switch
    {
        EventSourceIdSequencingKey eventSourceId => FromEventSourceId(eventSourceId),
        ValueSequencingKey value => FromValue(value),
        UnresolvedSequencingKey unresolved => FromUnresolved(unresolved),
        _ => throw new InvalidSequencingKey("An event source id, value, or unresolved key is required.")
    };

    /// <summary>
    /// Attempts canonicalization and reports the expected rejection without a substitute key.
    /// </summary>
    /// <param name="key">The logical sequencing key.</param>
    /// <param name="canonical">The canonical key on success.</param>
    /// <param name="rejection">The rejection reason, or none on success.</param>
    /// <returns>Whether the key was accepted.</returns>
    public static bool TryFrom(SequencingKey key, [NotNullWhen(true)] out CanonicalSequencingKey? canonical, out CanonicalSequencingKeyRejection rejection)
    {
        canonical = null;
        try
        {
            canonical = From(key);
            rejection = CanonicalSequencingKeyRejection.None;

            return true;
        }
        catch (InvalidSequencingKey)
        {
            rejection = CanonicalSequencingKeyRejection.InvalidKey;
        }
        catch (InvalidSequencingKeyText)
        {
            rejection = CanonicalSequencingKeyRejection.InvalidText;
        }
        catch (SequencingKeyTooLarge)
        {
            rejection = CanonicalSequencingKeyRejection.TooLarge;
        }

        return false;
    }

    /// <summary>
    /// Restores bytes only after strict canonical format validation.
    /// </summary>
    /// <param name="bytes">The stored bytes.</param>
    /// <returns>An immutable copy of the validated key.</returns>
    public static CanonicalSequencingKey FromStored(ReadOnlySpan<byte> bytes)
    {
        _ = DescribeBytes(bytes);

        return new(bytes);
    }

    /// <summary>
    /// Describes the key's parts, unresolved text, or digest for display; original event source ids remain on the logical key.
    /// </summary>
    /// <returns>The non-lossy display description.</returns>
    public SequencingKeyDescription Describe() => DescribeBytes(Span);

    /// <summary>
    /// Exports a fresh copy of the canonical bytes.
    /// </summary>
    /// <returns>A mutable copy which cannot affect this key.</returns>
    public byte[] ToArray() => _bytes.ToArray();

    /// <inheritdoc/>
    public bool Equals(CanonicalSequencingKey? other) => other is not null && Span.SequenceEqual(other.Span);

    /// <inheritdoc/>
    public override int GetHashCode() => unchecked((int)SequencingKeySliceHash.Hash(Span));

    static CanonicalSequencingKey FromEventSourceId(EventSourceIdSequencingKey key)
    {
        var text = key.EventSourceId.Value;
        if (SequencingKeyText.TryByteCount(text, MaximumLength - 5, out var length))
        {
            var bytes = new byte[5 + length];
            bytes[0] = (byte)SequencingKeyKind.Value;
            var offset = 1;
            WriteText(text, length, bytes, ref offset);

            return new(bytes);
        }

        var digestBytes = new byte[33];
        digestBytes[0] = (byte)SequencingKeyKind.EventSourceIdDigest;
        SequencingKeyText.Utf16Digest(text).CopyTo(digestBytes, 1);

        return new(digestBytes);
    }

    static CanonicalSequencingKey FromValue(ValueSequencingKey key)
    {
        Span<int> lengths = stackalloc int[key.Parts.Length];
        var total = 1;
        for (var index = 0; index < key.Parts.Length; index++)
        {
            var length = SequencingKeyText.ByteCount(key.Parts[index]);
            if (length > MaximumLength - total - 4)
            {
                throw new SequencingKeyTooLarge();
            }

            lengths[index] = length;
            total += 4 + length;
        }

        var bytes = new byte[total];
        bytes[0] = (byte)SequencingKeyKind.Value;
        var offset = 1;
        for (var index = 0; index < key.Parts.Length; index++)
        {
            WriteText(key.Parts[index], lengths[index], bytes, ref offset);
        }

        return new(bytes);
    }

    static CanonicalSequencingKey FromUnresolved(UnresolvedSequencingKey key)
    {
        var text = key.EventType.Value;
        if (SequencingKeyText.TryByteCount(text, MaximumLength - 6, out var length))
        {
            var bytes = new byte[6 + length];
            bytes[0] = (byte)SequencingKeyKind.Unresolved;
            bytes[1] = (byte)UnresolvedKeyForm.Text;
            var offset = 2;
            WriteText(text, length, bytes, ref offset);

            return new(bytes);
        }

        var digestBytes = new byte[34];
        digestBytes[0] = (byte)SequencingKeyKind.Unresolved;
        digestBytes[1] = (byte)UnresolvedKeyForm.Utf16Digest;
        SequencingKeyText.Utf16Digest(text).CopyTo(digestBytes, 2);

        return new(digestBytes);
    }

    static void WriteText(string text, int length, Span<byte> bytes, ref int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes[offset..], (uint)length);
        offset += 4;
        SequencingKeyText.Write(text, bytes.Slice(offset, length));
        offset += length;
    }

    static SequencingKeyDescription DescribeBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty || bytes.Length > MaximumLength)
        {
            throw new InvalidCanonicalSequencingKey("The canonical length is outside its supported range.");
        }

        return (SequencingKeyKind)bytes[0] switch
        {
            SequencingKeyKind.Value => DescribeValue(bytes),
            SequencingKeyKind.Unresolved => DescribeUnresolved(bytes),
            SequencingKeyKind.EventSourceIdDigest => DescribeEventSourceIdDigest(bytes),
            _ => throw new InvalidCanonicalSequencingKey("The kind byte is unknown.")
        };
    }

    static SequencingKeyDescription DescribeValue(ReadOnlySpan<byte> bytes)
    {
        var parts = ImmutableArray.CreateBuilder<string>();
        var offset = 1;
        while (offset < bytes.Length)
        {
            if (parts.Count == 16)
            {
                throw new InvalidCanonicalSequencingKey("A value key cannot contain more than sixteen parts.");
            }

            parts.Add(ReadText(bytes, ref offset));
        }

        if (parts.Count == 0)
        {
            throw new InvalidCanonicalSequencingKey("A value key requires at least one part.");
        }

        return new(SequencingKeyKind.Value, parts.ToImmutable(), null, null);
    }

    static SequencingKeyDescription DescribeUnresolved(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            throw new InvalidCanonicalSequencingKey("The unresolved form byte is missing.");
        }

        switch ((UnresolvedKeyForm)bytes[1])
        {
            case UnresolvedKeyForm.Text:
                var offset = 2;
                var text = ReadText(bytes, ref offset);
                if (offset != bytes.Length)
                {
                    throw new InvalidCanonicalSequencingKey("The unresolved text has trailing bytes.");
                }

                return new(SequencingKeyKind.Unresolved, [], text, null);
            case UnresolvedKeyForm.Utf16Digest:
                if (bytes.Length != 34)
                {
                    throw new InvalidCanonicalSequencingKey("The UTF-16 digest must contain exactly thirty-two bytes.");
                }

                return new(SequencingKeyKind.Unresolved, [], null, Convert.ToHexStringLower(bytes[2..]));
            default:
                throw new InvalidCanonicalSequencingKey("The unresolved form byte is unknown.");
        }
    }

    static SequencingKeyDescription DescribeEventSourceIdDigest(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 33)
        {
            throw new InvalidCanonicalSequencingKey("The event source id digest must contain exactly thirty-two bytes.");
        }

        return new(SequencingKeyKind.EventSourceIdDigest, [], null, Convert.ToHexStringLower(bytes[1..]));
    }

    static string ReadText(ReadOnlySpan<byte> bytes, ref int offset)
    {
        if (bytes.Length - offset < 4)
        {
            throw new InvalidCanonicalSequencingKey("The text length prefix is truncated.");
        }

        var length = BinaryPrimitives.ReadUInt32BigEndian(bytes[offset..]);
        offset += 4;
        if (length > bytes.Length - offset)
        {
            throw new InvalidCanonicalSequencingKey("The text payload is truncated.");
        }

        var text = SequencingKeyText.Decode(bytes.Slice(offset, (int)length));
        offset += (int)length;

        return text;
    }
}
