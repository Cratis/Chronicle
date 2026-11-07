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

    [LoggerMessage(LogLevel.Warning, "The encryption key store could not be inspected for keys stored under earlier names")]
    internal static partial void EncryptionKeyInspectionFailed(this ILogger logger, Exception exception);
}
