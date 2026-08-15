// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_getting_or_adding_a_key.given;

public class an_in_memory_key_store : Specification
{
    protected InMemoryEncryptionKeyStorage _store;
    protected EncryptionKeyIdentifier _identifier;

    void Establish()
    {
        _store = new InMemoryEncryptionKeyStorage();
        _identifier = new EncryptionKeyIdentifier(Guid.NewGuid().ToString());
    }

    protected static EncryptionKey KeyNamed(string name) =>
        new(Encoding.UTF8.GetBytes($"{name}-public"), Encoding.UTF8.GetBytes($"{name}-private"));

    protected async Task<int> RevisionCount()
    {
        var count = 0;
        for (var revision = 1u; revision <= 8; revision++)
        {
            if (await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKeyRevision(revision)))
            {
                count++;
            }
        }

        return count;
    }
}
