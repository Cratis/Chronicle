// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Log messages for <see cref="VaultEncryptionKeyStorage"/>. No message carries an identifier, event store or namespace.
/// </summary>
internal static partial class VaultEncryptionKeyStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "An encryption key stored under an earlier name cannot be addressed unambiguously and was not used. Run the key upgrade; see the compliance storage documentation.")]
    internal static partial void KeyNeedsUpgrade(this ILogger<VaultEncryptionKeyStorage> logger);
}
