// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyScopeCatalog"/> over <see cref="IStorage"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal class StorageScopeCatalog(IStorage storage) : IEncryptionKeyScopeCatalog
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegacyScope>> GetScopes()
    {
        var scopes = new List<LegacyScope>();
        foreach (var eventStore in await storage.GetEventStores())
        {
            foreach (var state in await storage.GetEventStore(eventStore).Namespaces.GetAll())
            {
                scopes.Add(new(eventStore.Value, state.Name.Value));
            }
        }

        return scopes;
    }
}
