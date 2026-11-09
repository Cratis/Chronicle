// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_saving_a_key;

public class and_authorization_wins_an_insert_race : an_insert_conflict_after_authorization
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => _storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new EncryptionKey([1], [2])));

    [Fact] void should_reject_the_stale_save() => _result.ShouldBeOfExactType<EncryptionKeyFenceChanged>();
    [Fact] void should_not_retry_in_the_new_lifecycle() => _writes.ShouldEqual(1);
}
