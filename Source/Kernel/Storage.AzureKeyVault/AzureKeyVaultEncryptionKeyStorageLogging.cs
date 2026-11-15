// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Log messages for <see cref="AzureKeyVaultEncryptionKeyStorage"/>. No message carries an identifier, event store or namespace.
/// </summary>
internal static partial class AzureKeyVaultEncryptionKeyStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "An encryption key stored under an earlier name cannot be addressed unambiguously and was not used. Run the key upgrade; see the compliance storage documentation.")]
    internal static partial void KeyNeedsUpgrade(this ILogger<AzureKeyVaultEncryptionKeyStorage> logger);

    [LoggerMessage(LogLevel.Warning, "An encryption key is disabled in the key store and was not used. Enable it, or erase it, to continue; see the compliance storage documentation.")]
    internal static partial void KeyUnavailable(this ILogger<AzureKeyVaultEncryptionKeyStorage> logger);
}
