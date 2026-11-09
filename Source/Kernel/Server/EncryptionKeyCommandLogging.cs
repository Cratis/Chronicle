// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Log messages for <see cref="EncryptionKeyCommandRunner"/>. Counts only; no message carries an identifier.
/// </summary>
internal static partial class EncryptionKeyCommandLogging
{
    [LoggerMessage(LogLevel.Information, "No encryption keys stored under earlier names were found")]
    internal static partial void NoEarlierEncryptionKeys(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Encryption keys stored under earlier names were found: at least {Keys} keys, {Erasures} erasure records and {Shapes} entries that cannot be addressed unambiguously (complete: {Complete}). Operations on the affected keys are refused until the key upgrade is run; see the compliance storage documentation")]
    internal static partial void EarlierEncryptionKeysFound(this ILogger logger, long keys, long erasures, long shapes, bool complete);

    [LoggerMessage(LogLevel.Error, "The encryption key layout is in conflict with earlier material or could not be verified, so the server will not start. See the compliance storage documentation")]
    internal static partial void EncryptionKeyLayoutUnusable(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Encryption keys stored under earlier names still need the key upgrade and the configured legacy key handling refuses them, so the server will not start. Run {MigrateFlag}; see the compliance storage documentation")]
    internal static partial void EarlierEncryptionKeysRefused(this ILogger logger, string migrateFlag);

    [LoggerMessage(LogLevel.Information, "Encryption key migration (dry run: {DryRun}): {Scanned} scanned, {AlreadyMigrated} already migrated, {Copied} keys copied, {ErasureRecordsCopied} erasure records copied, {ConflictsKept} conflicts kept, {Ambiguous} entries that cannot be addressed unambiguously, {Failed} failed, {Verified} verified, {Removed} removed under earlier names (complete: {Complete})")]
    internal static partial void EncryptionKeysMigrated(this ILogger logger, bool dryRun, long scanned, long alreadyMigrated, long copied, long erasureRecordsCopied, long conflictsKept, long ambiguous, long failed, long verified, long removed, bool complete);

    [LoggerMessage(LogLevel.Error, "The encryption key migration failed with a failure of kind {Kind}. Nothing already copied is lost; running it again resumes")]
    internal static partial void EncryptionKeyMigrationFailed(this ILogger logger, string kind);

    [LoggerMessage(LogLevel.Warning, "The encryption key store could not be inspected for keys stored under earlier names (failure category: {Category})")]
    internal static partial void EncryptionKeyInspectionFailed(this ILogger logger, string category);
}
