// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;

public class an_insert_conflict_after_authorization : racing_encryption_key_collections
{
    protected int _writes;

    void Establish() =>
        _keys.InsertOneAsync(Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _writes++;
                _fence = new("identifier", 1u, [], true, Version: 2, Lifecycle: 2, ErasedLifecycle: 1);
                return Task.FromException(duplicate_keys.Duplicate());
            });
}
