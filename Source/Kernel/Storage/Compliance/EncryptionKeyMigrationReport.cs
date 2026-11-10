// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The result of migrating encryption keys stored under earlier names. Holds counts only, never identifiers.
/// </summary>
/// <param name="DryRun">Whether the run only counted what it would do.</param>
/// <param name="LayoutUnusable">Whether the layout of the key store is in conflict or could not be verified, so nothing was done.</param>
/// <param name="Complete">Whether every entry under earlier names was looked at.</param>
/// <param name="Scanned">The number of entries found under earlier names.</param>
/// <param name="AlreadyMigrated">The number of entries already present, with the same content, under the current names.</param>
/// <param name="Copied">The number of keys copied to the current names, or that would be copied in a dry run.</param>
/// <param name="ErasureRecordsCopied">The number of erasure records copied to the current names, or that would be copied in a dry run.</param>
/// <param name="ConflictsKept">The number of entries left as they are for review: different content under the current names, a key covered by an erasure, or an entry that changed during the run.</param>
/// <param name="AmbiguousForReview">The number of entries that cannot be tied to one address, and are left as they are.</param>
/// <param name="Failed">The number of failures: an error talking to the key store, or a copy that did not verify.</param>
/// <param name="Verified">The number of copies verified to have the same content as the entry under the earlier name.</param>
/// <param name="LegacyRemoved">The number of entries under earlier names that were removed after their copies were verified.</param>
public record EncryptionKeyMigrationReport(
    bool DryRun,
    bool LayoutUnusable,
    bool Complete,
    long Scanned,
    long AlreadyMigrated,
    long Copied,
    long ErasureRecordsCopied,
    long ConflictsKept,
    long AmbiguousForReview,
    long Failed,
    long Verified,
    long LegacyRemoved)
{
    /// <summary>
    /// Gets whether another migration owns the mount, or an operator must clear a stale lock.
    /// </summary>
    public bool MigrationLocked { get; init; }

    /// <summary>
    /// Gets the number of entries left as they are because they carry an expiry, a not-before time, a content type, tags, a disabled version or more than one version. Counted in <see cref="ConflictsKept"/> too.
    /// </summary>
    public long HeldForAttributeDecision { get; init; }

    /// <summary>
    /// Gets the number of entries removed under earlier names whose deleted copies the key store still holds, because it refused to purge them.
    /// </summary>
    public long LegacyRemovedNotPurged { get; init; }

    /// <summary>
    /// Gets the number of event store and namespace pairs the kernel knows of that were not listed. Only reported: nothing is added to the list.
    /// </summary>
    public long ScopesMissingFromList { get; init; }

    /// <summary>
    /// Gets whether anything is left for an operator to review.
    /// </summary>
    public bool NeedsReview => ConflictsKept + AmbiguousForReview + LegacyRemovedNotPurged > 0;

    /// <summary>
    /// Gets whether a dry run found anything it would copy.
    /// </summary>
    public bool WouldCopy => DryRun && Copied + ErasureRecordsCopied > 0;
}
