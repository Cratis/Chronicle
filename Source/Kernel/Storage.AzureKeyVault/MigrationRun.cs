// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The counts of one run of <see cref="AzureKeyVaultKeyStoreMigrator"/>.
/// </summary>
/// <param name="dryRun">Whether the run only counts.</param>
internal sealed class MigrationRun(bool dryRun)
{
    /// <summary>
    /// Gets whether the run only counts.
    /// </summary>
    public bool DryRun { get; } = dryRun;

    /// <summary>
    /// Gets or sets the number of earlier secrets seen.
    /// </summary>
    public long Scanned { get; set; }

    /// <summary>
    /// Gets or sets the number of entries whose copy already existed.
    /// </summary>
    public long AlreadyMigrated { get; set; }

    /// <summary>
    /// Gets or sets the number of keys copied.
    /// </summary>
    public long Copied { get; set; }

    /// <summary>
    /// Gets or sets the number of erasure records copied.
    /// </summary>
    public long ErasureRecordsCopied { get; set; }

    /// <summary>
    /// Gets or sets the number of entries kept because of a conflict.
    /// </summary>
    public long ConflictsKept { get; set; }

    /// <summary>
    /// Gets or sets the number of entries held because of their attributes.
    /// </summary>
    public long HeldForAttributeDecision { get; set; }

    /// <summary>
    /// Gets or sets the number of names that could not be tied to one address.
    /// </summary>
    public long AmbiguousForReview { get; set; }

    /// <summary>
    /// Gets or sets the number of failures.
    /// </summary>
    public long Failed { get; set; }

    /// <summary>
    /// Gets or sets the number of earlier secrets removed.
    /// </summary>
    public long LegacyRemoved { get; set; }

    /// <summary>
    /// Gets or sets the number of earlier secrets removed that the key store still holds as recoverable.
    /// </summary>
    public long LegacyRemovedNotPurged { get; set; }

    /// <summary>
    /// Gets or sets the number of known scopes that were not listed.
    /// </summary>
    public long ScopesMissingFromList { get; set; }

    /// <summary>
    /// Gets the copies that were verified.
    /// </summary>
    public List<VerifiedCopy> Verified { get; } = [];

    /// <summary>
    /// Gets whether nothing failed and nothing was kept, so verified earlier secrets may be removed.
    /// </summary>
    public bool Clean => Failed == 0 && ConflictsKept == 0;

    /// <summary>
    /// Counts the outcome of one entry.
    /// </summary>
    /// <param name="isErasure">Whether the entry is an erasure record.</param>
    /// <param name="outcome">The <see cref="CopyOutcome"/>.</param>
    public void Count(bool isErasure, CopyOutcome outcome)
    {
        switch (outcome)
        {
            case CopyOutcome.AlreadyMigrated:
                AlreadyMigrated++;
                break;
            case CopyOutcome.Copied when isErasure:
                ErasureRecordsCopied++;
                break;
            case CopyOutcome.Copied:
                Copied++;
                break;
            case CopyOutcome.Held:
                HeldForAttributeDecision++;
                ConflictsKept++;
                break;
            default:
                ConflictsKept++;
                break;
        }
    }

    /// <summary>
    /// Creates the report.
    /// </summary>
    /// <param name="complete">Whether the run finished everything it set out to do.</param>
    /// <returns>The <see cref="EncryptionKeyMigrationReport"/>.</returns>
    public EncryptionKeyMigrationReport Report(bool complete) => new(
        DryRun,
        false,
        complete,
        Scanned,
        AlreadyMigrated,
        Copied,
        ErasureRecordsCopied,
        ConflictsKept,
        AmbiguousForReview,
        Failed,
        Verified.Count,
        LegacyRemoved)
    {
        HeldForAttributeDecision = HeldForAttributeDecision,
        LegacyRemovedNotPurged = LegacyRemovedNotPurged,
        ScopesMissingFromList = ScopesMissingFromList
    };
}
