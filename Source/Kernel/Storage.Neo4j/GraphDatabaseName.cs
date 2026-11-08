// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Implements the algorithm that names the Neo4j database of an event store and a namespace.
/// </summary>
/// <remarks>
/// <para>
/// Cratis Arc uses the same algorithm to read the database the sink writes, so the two must never diverge. The steps are:
/// </para>
/// <list type="number">
/// <item>Compose <c language="text">[prefix-]eventStore[-namespace]</c>. The <c language="text">Default</c> namespace (exact, case-sensitive), null or empty adds nothing. This is the unsanitized name.</item>
/// <item>Lower-case it, then replace every UTF-16 character outside <c language="text">[a-z0-9-]</c> with <c language="text">-</c>.</item>
/// <item>If the result does not start with a letter, prefix <c language="text">g-</c>.</item>
/// <item>If sanitizing changed the lower-cased name, or the result is longer than 63 or shorter than 3 characters, truncate to 54 characters and
/// append <c language="text">-</c> and the first 8 lower-case hex characters of the SHA-256 of the unsanitized name as UTF-8.</item>
/// </list>
/// Lower-casing alone triggers no hash, so <c language="text">Acme</c> and <c language="text">acme</c> share a database on purpose.
/// </remarks>
public static class GraphDatabaseName
{
    /// <summary>
    /// The name of the namespace that adds nothing to the database name.
    /// </summary>
    public const string DefaultNamespace = "Default";

    /// <summary>
    /// The longest a database name is allowed to be.
    /// </summary>
    public const int MaxLength = 63;

    /// <summary>
    /// The shortest a database name is allowed to be.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// The length a name is truncated to before the hash suffix is appended.
    /// </summary>
    public const int TruncatedLength = 54;

    /// <summary>
    /// The prefix given to a name that does not start with a letter.
    /// </summary>
    public const string LetterPrefix = "g-";

    const int HashLength = 8;

    /// <summary>
    /// Get the name of the database.
    /// </summary>
    /// <param name="prefix">Optional prefix. Null or empty adds nothing.</param>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace. Null, empty or <see cref="DefaultNamespace"/> adds nothing.</param>
    /// <returns>The name of the database.</returns>
    public static string For(string? prefix, string eventStore, string? @namespace)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventStore);

        var composed = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            composed.Append(prefix).Append('-');
        }

        composed.Append(eventStore);
        if (!string.IsNullOrEmpty(@namespace) && @namespace != DefaultNamespace)
        {
            composed.Append('-').Append(@namespace);
        }

        var unsanitized = composed.ToString();
        var lowerCased = unsanitized.ToLowerInvariant();
        var sanitized = Sanitize(lowerCased);
        var changed = sanitized != lowerCased;

        if (!char.IsAsciiLetterLower(sanitized[0]))
        {
            sanitized = LetterPrefix + sanitized;
        }

        if (!changed && sanitized.Length is >= MinLength and <= MaxLength)
        {
            return sanitized;
        }

        var truncated = sanitized.Length > TruncatedLength ? sanitized[..TruncatedLength] : sanitized;
        return $"{truncated}-{Hash(unsanitized)}";
    }

    static string Sanitize(string value) =>
        string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' ? c : '-';
            }
        });

    static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..HashLength].ToLowerInvariant();
}
