// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Runs the inspection of an encryption key store for keys stored under earlier names.
/// </summary>
internal static class EncryptionKeyCommandRunner
{
    /// <summary>
    /// The exit code when there is nothing stored under earlier names.
    /// </summary>
    internal const int Clean = 0;

    /// <summary>
    /// The exit code for an unexpected error.
    /// </summary>
    internal const int Failed = 1;

    /// <summary>
    /// The exit code when keys stored under earlier names need the key upgrade.
    /// </summary>
    internal const int UpgradeNeeded = 2;

    /// <summary>
    /// The exit code when the layout of the key store is in conflict or could not be verified.
    /// </summary>
    internal const int LayoutUnusable = 4;

    /// <summary>
    /// Inspects the key store.
    /// </summary>
    /// <param name="check">The <see cref="IEncryptionKeyStoreUpgradeCheck"/>, or null when the key storage has none.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="command">The <see cref="EncryptionKeyCommand"/>.</param>
    /// <param name="output">Where a verification prints its counts.</param>
    /// <returns>The exit code, or null when the server should carry on starting.</returns>
    internal static async Task<int?> Run(IEncryptionKeyStoreUpgradeCheck? check, ILogger logger, EncryptionKeyCommand command, TextWriter output)
    {
        if (check is null)
        {
            if (command.Verify)
            {
                await output.WriteLineAsync("The encryption key storage has no keys stored under earlier names to inspect.");
                return Clean;
            }

            return null;
        }

        EncryptionKeyStoreUpgradeStatus status;
        try
        {
            status = await check.Inspect(command.Verify);
        }
        catch (Exception exception)
        {
            logger.EncryptionKeyInspectionFailed(exception);
            return command.Verify ? Failed : null;
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
            return null;
        }

        await output.WriteLineAsync($"Earlier keys: {status.EarlierNameKeys}; earlier erasure records: {status.EarlierNameErasures}; entries that cannot be addressed unambiguously: {status.UnresolvedShapes}; complete: {status.Complete}");
        return status.EarlierMaterialPresent ? UpgradeNeeded : Clean;
    }
}
