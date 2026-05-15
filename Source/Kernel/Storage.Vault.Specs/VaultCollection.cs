// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Collection fixture that shares a single <see cref="VaultFixture"/> across all Vault integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class VaultCollection : ICollectionFixture<VaultFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Vault";
}
