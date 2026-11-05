// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Unicode;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Encodes the parts of an encryption key address into single, safe segments of a Vault secret path.
/// </summary>
/// <remarks>
/// <para>
/// Letters, digits and <c language="csharp">- _ . @ + = , : $</c> are kept as they are. Every other character - path
/// separators, query and fragment markers, escapes, whitespace, control characters, every non-ASCII character and
/// the escape character <c language="csharp">~</c> itself - is written as <c language="csharp">~XX</c> for each of its UTF-8
/// bytes, in uppercase hexadecimal. A trailing <c language="csharp">.</c> is written the same way, so a segment can
/// never be <c language="csharp">.</c> or <c language="csharp">..</c>. The encoding is reversible, so two different texts never
/// share a segment, and it never changes text that needs no escaping.
/// </para>
/// <para>
/// A segment never starts with <c language="csharp">~</c> followed by something other than two hexadecimal digits. Such segments
/// are therefore free to be used for secrets that belong to the store itself.
/// </para>
/// <para>
/// New paths live under <see cref="Root"/>. Paths without that root are from earlier versions, and are read only for
/// addresses they can belong to alone.
/// </para>
/// </remarks>
internal static class VaultPathSegments
{
    /// <summary>
    /// The largest number of UTF-8 bytes a segment may be made of.
    /// </summary>
    public const int MaxBytes = 1024;

    /// <summary>
    /// The first segment of every path written by this version.
    /// </summary>
    public const string Root = "k1";

    /// <summary>
    /// The name of the secret, at the root of the mount, that records the layout in use.
    /// </summary>
    public const string LayoutMarker = "~layout";

    const char Escape = '~';
    const string UpperHex = "0123456789ABCDEF";

    /// <summary>
    /// Encodes a value as a path segment.
    /// </summary>
    /// <param name="value">The text to encode.</param>
    /// <param name="component">The kind of component the text is, for example <c language="csharp">identifier</c>. Used in the error only.</param>
    /// <returns>The segment - <paramref name="value"/> itself when it needs no escaping.</returns>
    /// <exception cref="InvalidEncryptionKeyAddress">The value is empty, too long or not valid text.</exception>
    public static string Encode(string value, string component)
    {
        if (value.Length == 0)
        {
            throw new InvalidEncryptionKeyAddress(component);
        }

        var needsEscaping = false;
        for (var i = 0; i < value.Length; i++)
        {
            if (!IsLiteral(value[i]))
            {
                needsEscaping = true;
                break;
            }
        }

        if (!needsEscaping && value[^1] != '.')
        {
            return value.Length <= MaxBytes ? value : throw new InvalidEncryptionKeyAddress(component);
        }

        return Escaped(value, component);
    }

    /// <summary>
    /// Gets the directory an address was stored in before segments were encoded, when the address may still use it.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The earlier directory, or null when the address does not use it.</returns>
    /// <remarks>
    /// An address is looked up under its earlier directory only when every component is made of the characters kept as
    /// they are, <c language="csharp">~</c>, and letters, marks and digits outside ASCII, and does not end in
    /// <c language="csharp">.</c>. Such a directory has exactly three segments, so it can belong to one address only.
    /// </remarks>
    public static string? LegacyDirectory(string eventStore, string eventStoreNamespace, string identifier) =>
        IsEligibleForLegacyDirectory(eventStore, eventStoreNamespace, identifier)
            ? RawDirectory(eventStore, eventStoreNamespace, identifier)
            : null;

    /// <summary>
    /// Gets the directory an earlier version addressed, exactly as it was built then.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The earlier directory.</returns>
    public static string RawDirectory(string eventStore, string eventStoreNamespace, string identifier) =>
        $"{eventStore}/{eventStoreNamespace}/{identifier}";

    static bool IsEligibleForLegacyDirectory(string eventStore, string eventStoreNamespace, string identifier) =>
        IsEligibleLegacyComponent(eventStore) && IsEligibleLegacyComponent(eventStoreNamespace) && IsEligibleLegacyComponent(identifier);

    static bool IsEligibleLegacyComponent(string component)
    {
        if (component.Length == 0 || component[^1] == '.')
        {
            return false;
        }

        // Invalid UTF-16 is enumerated as the replacement character, which is neither ASCII nor a letter, mark or digit.
        foreach (var rune in component.EnumerateRunes())
        {
            if (rune.IsAscii)
            {
                if (rune.Value != Escape && !IsLiteral((char)rune.Value))
                {
                    return false;
                }

                continue;
            }

            if (!IsLetterMarkOrDigit(rune))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsLetterMarkOrDigit(Rune rune) =>
        Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or
            UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark or
            UnicodeCategory.DecimalDigitNumber;

    static bool IsLiteral(char character) =>
        character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or
            '-' or '_' or '.' or '@' or '+' or '=' or ',' or ':' or '$';

    static string Escaped(string value, string component)
    {
        var maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
        byte[]? rented = null;
        var utf8 = maxBytes <= 512
            ? stackalloc byte[512]
            : new Span<byte>(rented = ArrayPool<byte>.Shared.Rent(maxBytes));
        try
        {
            var status = Utf8.FromUtf16(value, utf8, out _, out var byteCount, replaceInvalidSequences: false);
            if (status != OperationStatus.Done || byteCount == 0 || byteCount > MaxBytes)
            {
                throw new InvalidEncryptionKeyAddress(component);
            }

            return Build(utf8[..byteCount]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    static string Build(ReadOnlySpan<byte> utf8)
    {
        var length = 0;
        for (var i = 0; i < utf8.Length; i++)
        {
            length += MustEscape(utf8, i) ? 3 : 1;
        }

        return string.Create(length, utf8.ToArray(), static (chars, bytes) =>
        {
            var position = 0;
            for (var i = 0; i < bytes.Length; i++)
            {
                if (MustEscape(bytes, i))
                {
                    chars[position++] = Escape;
                    chars[position++] = UpperHex[bytes[i] >> 4];
                    chars[position++] = UpperHex[bytes[i] & 0xF];
                }
                else
                {
                    chars[position++] = (char)bytes[i];
                }
            }
        });
    }

    static bool MustEscape(ReadOnlySpan<byte> utf8, int index) =>
        utf8[index] >= 0x80 || !IsLiteral((char)utf8[index]) || (utf8[index] == '.' && index == utf8.Length - 1);
}
