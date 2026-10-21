// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Encodes ordinal sequencing key text without replacement or normalization.
/// </summary>
public static class SequencingKeyText
{
    static readonly UTF8Encoding _strict = new(false, true);

    /// <summary>
    /// Checks whether text contains only well-formed UTF-16 code unit sequences.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>Whether the text is well-formed.</returns>
    public static bool IsWellFormed(string text)
    {
        if (text is null)
        {
            return false;
        }

        for (var index = 0; index < text.Length; index++)
        {
            if (ByteLengthAt(text, ref index) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Counts the strict UTF-8 bytes of text.
    /// </summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The UTF-8 byte count.</returns>
    /// <exception cref="InvalidSequencingKeyText">The text is malformed or cannot be counted.</exception>
    public static int ByteCount(string text)
    {
        if (!TryByteCount(text, int.MaxValue, out var count))
        {
            throw new InvalidSequencingKeyText("Text must be well-formed and its UTF-8 byte count must fit in Int32.");
        }

        return count;
    }

    /// <summary>
    /// Decodes strict UTF-8 and verifies its byte-for-byte round trip.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded ordinal text.</returns>
    /// <exception cref="InvalidCanonicalSequencingKey">The bytes are not strict UTF-8.</exception>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var text = _strict.GetString(bytes);
            if (!_strict.GetBytes(text).AsSpan().SequenceEqual(bytes))
            {
                throw new InvalidCanonicalSequencingKey("Text does not round trip as strict UTF-8.");
            }

            return text;
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidCanonicalSequencingKey("Text is not strict UTF-8.");
        }
    }

    /// <summary>
    /// Exports the raw UTF-16 code units in little-endian order without a BOM or validation.
    /// </summary>
    /// <param name="text">The text whose code units are exported.</param>
    /// <returns>A fresh array containing the raw code units.</returns>
    /// <exception cref="InvalidSequencingKeyText">The code units cannot fit in a byte array.</exception>
    public static byte[] Utf16LittleEndian(string text)
    {
        if (text is null || text.Length > int.MaxValue / 2)
        {
            throw new InvalidSequencingKeyText("Raw code units must fit in a byte array.");
        }

        var bytes = new byte[text.Length * 2];
        WriteCodeUnits(text.AsSpan(), bytes);

        return bytes;
    }

    /// <summary>
    /// Writes previously validated text as strict UTF-8.
    /// </summary>
    /// <param name="text">Validated text.</param>
    /// <param name="bytes">The destination buffer.</param>
    /// <returns>The number of bytes written.</returns>
    internal static int Write(string text, Span<byte> bytes) => _strict.GetBytes(text.AsSpan(), bytes);

    /// <summary>
    /// Counts UTF-8 bytes up to a bound without allocating an encoded buffer.
    /// </summary>
    /// <param name="text">The text to count.</param>
    /// <param name="maximum">The inclusive byte count limit.</param>
    /// <param name="count">The byte count on success.</param>
    /// <returns>Whether the text is well-formed and within the limit.</returns>
    internal static bool TryByteCount(string text, int maximum, out int count)
    {
        count = 0;
        if (text is null)
        {
            return false;
        }

        for (var index = 0; index < text.Length; index++)
        {
            var length = ByteLengthAt(text, ref index);
            if (length < 0 || count > maximum - length)
            {
                return false;
            }

            count += length;
        }

        return true;
    }

    /// <summary>
    /// Hashes raw UTF-16 little-endian code units using a fixed-size buffer.
    /// </summary>
    /// <param name="text">The text to hash, including malformed code units.</param>
    /// <returns>The SHA-256 digest.</returns>
    internal static byte[] Utf16Digest(string text)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> buffer = stackalloc byte[512];
        for (var offset = 0; offset < text.Length;)
        {
            var length = Math.Min(buffer.Length / 2, text.Length - offset);
            var chunk = buffer[..(length * 2)];
            WriteCodeUnits(text.AsSpan(offset, length), chunk);
            hash.AppendData(chunk);
            offset += length;
        }

        return hash.GetHashAndReset();
    }

    static int ByteLengthAt(string text, ref int index)
    {
        var character = text[index];
        if (character <= 0x7F)
        {
            return 1;
        }

        if (character <= 0x7FF)
        {
            return 2;
        }

        if (!char.IsSurrogate(character))
        {
            return 3;
        }

        if (!char.IsHighSurrogate(character) || index + 1 == text.Length || !char.IsLowSurrogate(text[index + 1]))
        {
            return -1;
        }

        index++;

        return 4;
    }

    static void WriteCodeUnits(ReadOnlySpan<char> text, Span<byte> bytes)
    {
        for (var index = 0; index < text.Length; index++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes[(index * 2)..], text[index]);
        }
    }
}
