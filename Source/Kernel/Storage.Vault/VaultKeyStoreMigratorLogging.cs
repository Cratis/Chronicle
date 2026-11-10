// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Log messages for <see cref="VaultKeyStoreMigrator"/>. No message carries a path, an identifier or what the key store answered.
/// </summary>
internal static partial class VaultKeyStoreMigratorLogging
{
    [LoggerMessage(LogLevel.Error, "The encryption key migration stopped on a failure of kind {Kind}. Nothing already copied is lost; running it again resumes")]
    internal static partial void MigrationStopped(this ILogger<VaultKeyStoreMigrator> logger, string kind);
}
