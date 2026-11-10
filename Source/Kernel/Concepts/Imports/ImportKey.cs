// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Imports;

/// <summary>
/// Represents the key of an import session grain.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> the import belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the import belongs to.</param>
/// <param name="ImportId">The <see cref="ImportId"/>.</param>
public record ImportKey(EventStoreName EventStore, EventStoreNamespaceName Namespace, ImportId ImportId)
{
    /// <summary>
    /// Implicitly converts from <see cref="ImportKey"/> to string.
    /// </summary>
    /// <param name="key">The <see cref="ImportKey"/>.</param>
    public static implicit operator string(ImportKey key) => key.ToString();

    /// <summary>
    /// Implicitly converts from string to <see cref="ImportKey"/>.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    public static implicit operator ImportKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace, ImportId);

    /// <summary>
    /// Parses an <see cref="ImportKey"/> from a string.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <returns>The <see cref="ImportKey"/>.</returns>
    public static ImportKey Parse(string key) => KeyHelper.Parse<ImportKey>(key);
}
