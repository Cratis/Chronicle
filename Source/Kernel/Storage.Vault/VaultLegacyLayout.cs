// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Finds the secrets earlier versions stored, and compares them with their copies under <see cref="VaultPathSegments.Root"/>.
/// </summary>
internal static class VaultLegacyLayout
{
    /// <summary>
    /// The field every secret this version writes records its layout in.
    /// </summary>
    public const string LayoutField = "layout";

    /// <summary>
    /// Gets whether a root entry is an event store from an earlier version. Only the two names this version reserves are
    /// skipped, and only when they are exactly those: the root directory and the marker leaf. Anything else, whatever
    /// character it starts with, is an event store from an earlier version.
    /// </summary>
    /// <param name="entry">The entry listed at the root of the mount.</param>
    /// <returns>True when the entry is from an earlier version, false otherwise.</returns>
    public static bool IsEarlierEntry(string entry) => entry != $"{VaultPathSegments.Root}/" && entry != VaultPathSegments.LayoutMarker;

    /// <summary>
    /// Walks every secret below the given earlier root entries.
    /// </summary>
    /// <param name="store">The <see cref="IVaultSecretStore"/>.</param>
    /// <param name="earlierRoots">The earlier root entries to walk.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The secrets found.</returns>
    public static async Task<List<VaultLegacyEntry>> Walk(IVaultSecretStore store, IEnumerable<string> earlierRoots, CancellationToken cancellationToken)
    {
        var entries = new List<VaultLegacyEntry>();
        foreach (var root in earlierRoots)
        {
            await Walk(store, [root.TrimEnd('/')], entries, cancellationToken);
        }

        return entries;
    }

    /// <summary>
    /// Gets the data a copy under <see cref="VaultPathSegments.Root"/> is written with: the data of the earlier secret as
    /// it is, with the layout recorded.
    /// </summary>
    /// <param name="legacy">The earlier <see cref="VaultSecret"/>.</param>
    /// <returns>The data of the copy.</returns>
    public static Dictionary<string, string> CopyOf(VaultSecret legacy) =>
        new(legacy.Data, StringComparer.Ordinal) { [LayoutField] = VaultPathSegments.Root };

    /// <summary>
    /// Gets whether a secret under <see cref="VaultPathSegments.Root"/> holds the same content as an earlier secret. Every
    /// field but the layout must be present in both and equal; values are compared in constant time.
    /// </summary>
    /// <param name="legacy">The earlier <see cref="VaultSecret"/>.</param>
    /// <param name="current">The <see cref="VaultSecret"/> under <see cref="VaultPathSegments.Root"/>.</param>
    /// <returns>True when the content is the same, false otherwise.</returns>
    public static bool SameContent(VaultSecret legacy, VaultSecret current)
    {
        if (!legacy.OnlyText || !current.OnlyText)
        {
            return false;
        }

        var fields = legacy.Data.Keys.Where(field => field != LayoutField).ToHashSet(StringComparer.Ordinal);
        if (!fields.SetEquals(current.Data.Keys.Where(field => field != LayoutField)))
        {
            return false;
        }

        var same = true;
        foreach (var field in fields)
        {
            same &= CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacy.Data[field]),
                Encoding.UTF8.GetBytes(current.Data[field]));
        }

        return same;
    }

    static async Task Walk(IVaultSecretStore store, List<string> path, List<VaultLegacyEntry> entries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entry in await store.List(string.Join('/', path)))
        {
            var segments = new List<string>(path) { entry.TrimEnd('/') };
            if (entry.EndsWith('/'))
            {
                await Walk(store, segments, entries, cancellationToken);
            }
            else
            {
                entries.Add(VaultLegacyEntry.From(segments));
            }
        }
    }
}
