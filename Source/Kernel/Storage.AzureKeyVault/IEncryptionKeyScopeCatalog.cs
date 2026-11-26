// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Defines the event stores and namespaces the kernel knows of. Used only to cross-check what an operator listed.
/// </summary>
public interface IEncryptionKeyScopeCatalog
{
    /// <summary>
    /// Gets every event store and namespace pair the kernel knows of.
    /// </summary>
    /// <returns>The <see cref="LegacyScope"/> pairs.</returns>
    Task<IReadOnlyList<LegacyScope>> GetScopes();
}
