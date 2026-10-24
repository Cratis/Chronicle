// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Resolves indexed stamps and legacy ids without changing stored data.
/// </summary>
internal static class SqlCompositeKeyLookup
{
    /// <summary>
    /// Chooses a safe row to materialize from the bounded candidate query.
    /// </summary>
    /// <param name="candidates">Rows matching the stamp or legacy id.</param>
    /// <param name="encoding">The requested v1 encoding.</param>
    /// <param name="legacyId">The requested legacy id.</param>
    /// <param name="ambiguous">Whether the legacy id loses part boundaries.</param>
    /// <returns>The outcome and the id to load when found.</returns>
    internal static (CompositeKeyLookupOutcome Outcome, string? IdToLoad) Decide(
        IEnumerable<Candidate> candidates,
        string encoding,
        string legacyId,
        bool ambiguous)
    {
        var rows = candidates.ToArray();
        var stamped = rows.FirstOrDefault(_ => string.Equals(_.Stamp, encoding, StringComparison.Ordinal));
        if (stamped is not null) return (CompositeKeyLookupOutcome.Found, stamped.Id);

        var legacy = rows.FirstOrDefault(_ => string.Equals(_.Id, legacyId, StringComparison.Ordinal));
        if (legacy is null) return (CompositeKeyLookupOutcome.NotFound, null);
        if (CompositeKeyEncoding.IsEncoded(legacy.Stamp)) return (CompositeKeyLookupOutcome.KeyCollision, null);
        return ambiguous
            ? (CompositeKeyLookupOutcome.AmbiguousLegacyKey, null)
            : (CompositeKeyLookupOutcome.Found, legacy.Id);
    }

    /// <summary>
    /// An unmapped projection of a row's id and composite-key stamp.
    /// </summary>
    internal sealed class Candidate
    {
        /// <summary>Gets or sets the legacy primary key.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional composite-key stamp.</summary>
        public string? Stamp { get; set; }
    }
}
