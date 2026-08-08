// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use Azure Key Vault for compliance storage.
/// </summary>
public static class AzureKeyVaultChronicleBuilderExtensions
{
    /// <summary>
    /// Configures Chronicle to use Azure Key Vault for encryption key storage, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="Configuration.Encryption.Storage"/> is configured and its type is <c>azure-key-vault</c>,
    /// this method adds a <see cref="Storage.AzureKeyVault.AzureKeyVaultEncryptionKeyStorage"/> wrapped in a
    /// <see cref="CacheEncryptionKeyStorage"/> as <see cref="IEncryptionKeyStorage"/>, taking over from the
    /// default storage registration.
    /// With <see cref="Configuration.Encryption.MigrateFromDefaultStorage"/> set it becomes the primary of a
    /// composite over the default storage instead, so keys that only exist there keep being served and are moved
    /// into Azure Key Vault as they are read.
    /// If compliance encryption storage is not configured, or the type is not <c>azure-key-vault</c>, no changes are made.
    /// Authentication is performed via <see cref="DefaultAzureCredential"/>.
    /// </remarks>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithAzureKeyVaultComplianceStorage(this IChronicleBuilder builder, ChronicleOptions options)
    {
        var complianceStorage = options.Compliance.Encryption.Storage;

        if (complianceStorage is null ||
            !string.Equals(complianceStorage.Type, Storage.AzureKeyVault.StorageType.AzureKeyVault, StringComparison.OrdinalIgnoreCase))
        {
            return builder;
        }

        return builder.WithComplianceEncryptionKeyStorage(options, _ =>
        {
            var secretClient = new SecretClient(
                new Uri(complianceStorage.ConnectionDetails),
                new DefaultAzureCredential());

            return new CacheEncryptionKeyStorage(
                new Storage.AzureKeyVault.AzureKeyVaultEncryptionKeyStorage(secretClient));
        });
    }
}
