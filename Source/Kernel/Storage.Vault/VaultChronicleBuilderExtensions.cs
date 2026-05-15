// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use HashiCorp Vault for compliance storage.
/// </summary>
public static class VaultChronicleBuilderExtensions
{
    /// <summary>
    /// Configures Chronicle to use HashiCorp Vault for encryption key storage, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="Configuration.Compliance.Storage"/> is configured and its type is <c>vault</c>,
    /// this method adds a <see cref="Storage.Vault.VaultEncryptionKeyStorage"/> wrapped in a <see cref="CacheEncryptionKeyStorage"/>
    /// as <see cref="IEncryptionKeyStorage"/>. Because it is registered last, it overrides the default storage registration.
    /// If compliance storage is not configured, or the type is not <c>vault</c>, no changes are made.
    /// </remarks>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithVaultComplianceStorage(this IChronicleBuilder builder, ChronicleOptions options)
    {
        var complianceStorage = options.Compliance.Storage;

        if (complianceStorage is null ||
            !string.Equals(complianceStorage.Type, Storage.Vault.StorageType.Vault, StringComparison.OrdinalIgnoreCase))
        {
            return builder;
        }

        builder.Services.AddSingleton<IEncryptionKeyStorage>(_ =>
            new CacheEncryptionKeyStorage(
                new Storage.Vault.VaultEncryptionKeyStorage(complianceStorage.ConnectionDetails)));

        return builder;
    }
}
