// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Log messages for <see cref="AzureKeyVaultKeyStoreMigrator"/>. No message carries a name, an identifier, a value or what the key store answered.
/// </summary>
internal static partial class AzureKeyVaultKeyStoreMigratorLogging
{
    [LoggerMessage(LogLevel.Error, "The encryption key migration stopped on a failure of kind {Kind}. Nothing already copied is lost; running it again resumes")]
    internal static partial void MigrationStopped(this ILogger<AzureKeyVaultKeyStoreMigrator> logger, string kind);

    [LoggerMessage(LogLevel.Error, "No scopes are listed under Compliance:Encryption:LegacyKeyScopes, so entries under earlier names cannot be tied to an address and nothing was migrated")]
    internal static partial void NoScopesListed(this ILogger<AzureKeyVaultKeyStoreMigrator> logger);

    [LoggerMessage(LogLevel.Error, "{Count} listed scopes have no entries under earlier names. A listed scope must have a source; check the list")]
    internal static partial void ScopesWithoutSource(this ILogger<AzureKeyVaultKeyStoreMigrator> logger, long count);

    [LoggerMessage(LogLevel.Warning, "{Count} event store and namespace pairs the kernel knows of are not listed under Compliance:Encryption:LegacyKeyScopes. They were not added")]
    internal static partial void ScopesNotListed(this ILogger<AzureKeyVaultKeyStoreMigrator> logger, long count);
}
