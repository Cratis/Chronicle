// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

/// <summary>
/// Serializes the forward proof, independently of the existing Vault collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class LiveVaultCollection : ICollectionFixture<LiveVaultFixture>
{
    /// <summary>
    /// The collection name for filtering container-backed specs.
    /// </summary>
    public const string Name = "LiveVaultForwardMigration";
}
