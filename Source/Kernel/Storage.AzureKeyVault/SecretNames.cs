// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Derives the names of the secrets an encryption key address is stored under.
/// </summary>
/// <remarks>
/// <para>
/// Names are <c language="csharp">chronicle-k1--{hash}--{suffix}</c> where the hash is the lowercase hexadecimal SHA-256 of
/// the length-prefixed UTF-8 bytes of the event store, the namespace and the identifier. Every distinct
/// <c language="csharp">(eventStore, namespace, identifier)</c> triple therefore has its own name, whatever characters it
/// contains and however long it is, and a name never carries subject data.
/// </para>
/// <para>
/// Keys stored before this naming existed live under <c language="csharp">chronicle--{eventStore}--{namespace}--{identifier}--{suffix}</c>
/// with each component lowercased and reduced to letters, digits and single hyphens. That form cannot tell
/// <c language="csharp">Alice.X</c> from <c language="csharp">alice-x</c>, so it is only used for the triples
/// <see cref="LegacyPrefix"/> accepts, and never for writing.
/// </para>
/// </remarks>
internal static partial class SecretNames
{
    /// <summary>
    /// The prefix every secret name carries.
    /// </summary>
    public const string Prefix = "chronicle-k1";

    const string LegacyNamePrefix = "chronicle";
    const string DomainTag = "chronicle-key-name/v1";
    const string EncryptedValueSanitizedPrefix = "chronicle-encrypted-value";
    const string SubjectPrefix = EncryptedValueKeyIdentifiers.Marker + "subject$";
    const int LengthPrefixSize = sizeof(int);
    const int StackLimit = 1024;

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SanitizeRegex { get; }

    [GeneratedRegex(@"\A[a-z0-9]+(-[a-z0-9]+)*\z", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CanonicalRegex { get; }

    [GeneratedRegex(@"\A[A-Za-z0-9]+(-[A-Za-z0-9]+)*\z", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CanonicalScopeNameRegex { get; }

    /// <summary>
    /// Gets the prefix every secret name of an address starts with: <c language="csharp">chronicle-k1--{hash}--</c>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The prefix.</returns>
    /// <exception cref="InvalidEncryptionKeyAddress">A component is empty or not valid text.</exception>
    public static string KeyPrefix(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        $"{Prefix}--{HashOf(eventStore.Value, eventStoreNamespace.Value, identifier.Value)}--";

    /// <summary>
    /// Gets the prefix the secrets of an address were named with before hashing, when the address may still use them.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The earlier prefix, or null when the address is not eligible for it.</returns>
    public static string? LegacyPrefix(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        IsEligibleForLegacyNames(eventStore.Value, eventStoreNamespace.Value, identifier.Value)
            ? SanitizedPrefix(eventStore.Value, eventStoreNamespace.Value, identifier.Value)
            : null;

    /// <summary>
    /// Gets the prefix the secrets of an address were named with before hashing, when the address may not use them
    /// because other addresses were given the same prefix.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The earlier prefix, or null when the address is eligible for <see cref="LegacyPrefix"/>.</returns>
    public static string? UnresolvedLegacyPrefix(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        IsEligibleForLegacyNames(eventStore.Value, eventStoreNamespace.Value, identifier.Value)
            ? null
            : SanitizedPrefix(eventStore.Value, eventStoreNamespace.Value, identifier.Value);

    /// <summary>
    /// Hashes an exact event store name in the reserved internal-secret naming domain.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>The lowercase SHA-256 hash of its length-prefixed UTF-8 encoding.</returns>
    internal static string InternalSecretHash(EventStoreName eventStore)
    {
        const string domainTag = "chronicle-internal-secret-name/v1";
        var buffer = new byte[domainTag.Length + 1 + LengthPrefixSize + MaxUtf8Length(eventStore.Value)];
        var written = Encoding.ASCII.GetBytes(domainTag, buffer);
        buffer[written++] = 0;
        written = AppendUtf8(buffer, written, eventStore.Value, "event store");
        return ToLowerHex(SHA256.HashData(buffer.AsSpan(0, written)));
    }

    static string SanitizedPrefix(string eventStore, string eventStoreNamespace, string identifier) =>
        $"{LegacyNamePrefix}--{Sanitize(eventStore)}--{Sanitize(eventStoreNamespace)}--{Sanitize(identifier)}--";

    static bool IsEligibleForLegacyNames(string eventStore, string eventStoreNamespace, string identifier) =>
        (eventStore == EncryptedValueKeyIdentifiers.GlobalEventStore.Value || IsEligibleScopeName(eventStore)) &&
        (eventStoreNamespace == EncryptedValueKeyIdentifiers.GlobalNamespace.Value || IsEligibleScopeName(eventStoreNamespace)) &&
        IsEligibleIdentifier(identifier);

    /// <summary>
    /// An event store or namespace name takes part when it is made of letters, digits and single hyphens, and does not
    /// reduce to the reserved encrypted value prefix. Case is tolerated because the default namespace is "Default".
    /// </summary>
    /// <param name="name">The event store or namespace name.</param>
    /// <returns>True when the name may use earlier secret names.</returns>
    static bool IsEligibleScopeName(string name) =>
        CanonicalScopeNameRegex.IsMatch(name) &&
        !Sanitize(name).StartsWith(EncryptedValueSanitizedPrefix, StringComparison.Ordinal);

    /// <summary>
    /// An identifier takes part when it is strictly canonical (lowercase letters and digits with single hyphens) and does not
    /// reduce to the reserved encrypted value prefix, or when it is one of the identifiers built for encrypted values.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>True when the identifier may use earlier secret names.</returns>
    static bool IsEligibleIdentifier(string identifier) =>
        identifier == EncryptedValueKeyIdentifiers.ForNamespace().Value ||
        identifier == EncryptedValueKeyIdentifiers.ForGlobal().Value ||
        (identifier.StartsWith(SubjectPrefix, StringComparison.Ordinal) && IsCanonicalIdentifier(identifier[SubjectPrefix.Length..])) ||
        (IsCanonicalIdentifier(identifier) && !identifier.StartsWith(EncryptedValueSanitizedPrefix, StringComparison.Ordinal));

    static bool IsCanonicalIdentifier(string identifier) => CanonicalRegex.IsMatch(identifier);

    static string Sanitize(string input) =>
        SanitizeRegex.Replace(input.ToLowerInvariant(), "-").Trim('-');

    static string HashOf(string eventStore, string eventStoreNamespace, string identifier)
    {
        var maxLength = DomainTag.Length + 1 + (3 * LengthPrefixSize) +
            MaxUtf8Length(eventStore) + MaxUtf8Length(eventStoreNamespace) + MaxUtf8Length(identifier);
        byte[]? rented = null;
        var buffer = maxLength <= StackLimit
            ? stackalloc byte[StackLimit]
            : new Span<byte>(rented = ArrayPool<byte>.Shared.Rent(maxLength));
        try
        {
            var written = Encoding.ASCII.GetBytes(DomainTag, buffer);
            buffer[written++] = 0;
            written = Append(buffer, written, eventStore, "event store");
            written = Append(buffer, written, eventStoreNamespace, "namespace");
            written = Append(buffer, written, identifier, "identifier");

            Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(buffer[..written], digest);
            return ToLowerHex(digest);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    static int MaxUtf8Length(string value) => Encoding.UTF8.GetMaxByteCount(value.Length);

    static int Append(Span<byte> buffer, int offset, string value, string component)
    {
        if (value.Length == 0)
        {
            throw new InvalidEncryptionKeyAddress(component);
        }

        return AppendUtf8(buffer, offset, value, component);
    }

    static int AppendUtf8(Span<byte> buffer, int offset, string value, string component)
    {
        var status = Utf8.FromUtf16(value, buffer[(offset + LengthPrefixSize)..], out _, out var written, replaceInvalidSequences: false);
        if (status != OperationStatus.Done)
        {
            throw new InvalidEncryptionKeyAddress(component);
        }

        BinaryPrimitives.WriteInt32BigEndian(buffer[offset..], written);
        return offset + LengthPrefixSize + written;
    }

    static string ToLowerHex(ReadOnlySpan<byte> bytes)
    {
        const string digits = "0123456789abcdef";
        Span<char> chars = stackalloc char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = digits[bytes[i] >> 4];
            chars[(i * 2) + 1] = digits[bytes[i] & 0xF];
        }

        return new string(chars);
    }
}
