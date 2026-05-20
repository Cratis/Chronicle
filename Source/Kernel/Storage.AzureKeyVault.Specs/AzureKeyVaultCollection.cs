// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Collection fixture that shares a single <see cref="AzureKeyVaultFixture"/> across all Azure Key Vault integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class AzureKeyVaultCollection : ICollectionFixture<AzureKeyVaultFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "AzureKeyVault";
}
