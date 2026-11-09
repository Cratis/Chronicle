// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Runs the inspection and the migration of an encryption key store for keys stored under earlier names.
/// </summary>
internal static class EncryptionKeyCommandRunner
{
    /// <summary>
    /// The exit code when there is nothing stored under earlier names.
    /// </summary>
    internal const int Clean = 0;

    /// <summary>
    /// The exit code when keys stored under earlier names need the key upgrade, or entries are left for review.
    /// </summary>
    internal const int UpgradeNeeded = 2;

    /// <summary>
    /// The exit code when the layout of the key store is in conflict or could not be verified.
    /// </summary>
    internal const int LayoutUnusable = 4;

    /// <summary>
    /// The exit code when a migration failed to write or verify a copy, or stopped before it finished.
    /// </summary>
    internal const int MigrationFailed = 5;

    /// <summary>
    /// Inspects or migrates the key store.
    /// </summary>
    /// <param name="check">The <see cref="IEncryptionKeyStoreUpgradeCheck"/>, or null when the key storage has none.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="command">The <see cref="EncryptionKeyCommand"/>.</param>
    /// <param name="output">Where a verification or a migration prints its counts.</param>
    /// <param name="migrator">The <see cref="IEncryptionKeyStoreMigrator"/>, or null when the key storage has none.</param>
    /// <param name="handling">The <see cref="LegacyKeyHandling"/> for a normal start.</param>
    /// <returns>The exit code, or null when the server should carry on starting.</returns>
    internal static async Task<int?> Run(
        IEncryptionKeyStoreUpgradeCheck? check,
        ILogger logger,
        EncryptionKeyCommand command,
        TextWriter output,
        IEncryptionKeyStoreMigrator? migrator = null,
        LegacyKeyHandling handling = LegacyKeyHandling.Warn)
    {
        if (command.Migrate)
        {
            return await Migrate(migrator, logger, command, output);
        }

        if (check is null)
        {
            return await NothingToInspect(command, output);
        }

        return await Inspect(check, logger, command, output, handling == LegacyKeyHandling.Refuse);
    }

    static async Task<int?> NothingToInspect(EncryptionKeyCommand command, TextWriter output)
    {
        if (!command.Verify)
        {
            return null;
        }

        await output.WriteLineAsync("The encryption key storage has no keys stored under earlier names to inspect.");
        return Clean;
    }

    static async Task<int?> Inspect(IEncryptionKeyStoreUpgradeCheck check, ILogger logger, EncryptionKeyCommand command, TextWriter output, bool refuse)
    {
        EncryptionKeyStoreUpgradeStatus status;
        try
        {
            status = await check.Inspect(command.Verify || refuse);
        }
        catch (Exception exception)
        {
            // An inspection that did not finish proves nothing about the layout, whatever stopped it - a timeout, a server
            // error, a network failure, a cancellation or anything unexpected - so the server does not start on it.
            logger.EncryptionKeyInspectionFailed(exception switch
            {
                OperationCanceledException => "Cancelled",
                TimeoutException => "Timeout",
                HttpRequestException => "Network",
                _ => "InspectionFailed"
            });
            return LayoutUnusable;
        }

        if (status.LayoutUnusable)
        {
            logger.EncryptionKeyLayoutUnusable();
            return LayoutUnusable;
        }

        if (!status.EarlierMaterialPresent)
        {
            logger.NoEarlierEncryptionKeys();
        }
        else
        {
            logger.EarlierEncryptionKeysFound(status.EarlierNameKeys, status.EarlierNameErasures, status.UnresolvedShapes, status.Complete);
        }

        if (!command.Verify)
        {
            return StartOrRefuse(logger, status, refuse);
        }

        await output.WriteLineAsync($"Earlier keys: {status.EarlierNameKeys}; earlier erasure records: {status.EarlierNameErasures}; entries that cannot be addressed unambiguously: {status.UnresolvedShapes}; already migrated and kept: {status.MigratedLegacyRetained}; complete: {status.Complete}");
        return status.EarlierMaterialPresent ? UpgradeNeeded : Clean;
    }

    static int? StartOrRefuse(ILogger logger, EncryptionKeyStoreUpgradeStatus status, bool refuse)
    {
        if (!refuse || !status.EarlierMaterialPresent)
        {
            return null;
        }

        logger.EarlierEncryptionKeysRefused(EncryptionKeyCommand.MigrateFlag);
        return UpgradeNeeded;
    }

    static int ExitCodeFor(EncryptionKeyMigrationReport report)
    {
        if (report.Failed > 0 || !report.Complete)
        {
            return MigrationFailed;
        }

        return report.NeedsReview || report.WouldCopy ? UpgradeNeeded : Clean;
    }

    static async Task<int> Migrate(IEncryptionKeyStoreMigrator? migrator, ILogger logger, EncryptionKeyCommand command, TextWriter output)
    {
        if (migrator is null)
        {
            await output.WriteLineAsync("The encryption key storage has no keys stored under earlier names to migrate.");
            return Clean;
        }

        EncryptionKeyMigrationReport report;
        try
        {
            report = await migrator.Migrate(command.DryRun, command.RemoveMigratedLegacy);
        }
        catch (Exception exception)
        {
            // Only the kind of failure: what the key store answered may hold paths.
            logger.EncryptionKeyMigrationFailed(exception.GetType().Name);
            return MigrationFailed;
        }

        if (report.LayoutUnusable)
        {
            logger.EncryptionKeyLayoutUnusable();
            return LayoutUnusable;
        }

        logger.EncryptionKeysMigrated(report.DryRun, report.Scanned, report.AlreadyMigrated, report.Copied, report.ErasureRecordsCopied, report.ConflictsKept, report.AmbiguousForReview, report.Failed, report.Verified, report.LegacyRemoved, report.Complete);
        await output.WriteLineAsync(
            $"Dry run: {report.DryRun}; scanned: {report.Scanned}; already migrated: {report.AlreadyMigrated}; " +
            $"{(report.DryRun ? "keys that would be copied" : "keys copied")}: {report.Copied}; " +
            $"{(report.DryRun ? "erasure records that would be copied" : "erasure records copied")}: {report.ErasureRecordsCopied}; " +
            $"conflicts kept: {report.ConflictsKept}; entries that cannot be addressed unambiguously: {report.AmbiguousForReview}; " +
            $"failed: {report.Failed}; verified: {report.Verified}; removed under earlier names: {report.LegacyRemoved}; complete: {report.Complete}");

        return ExitCodeFor(report);
    }
}
