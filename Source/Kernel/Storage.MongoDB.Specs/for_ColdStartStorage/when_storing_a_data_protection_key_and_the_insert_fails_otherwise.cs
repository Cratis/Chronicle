// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_storing_a_data_protection_key_and_the_insert_fails_otherwise : a_data_protection_key_collection
{
    Exception _result;

    void Establish() => InsertFailsWith(new InvalidOperationException("boom"));

    async Task Because() => _result = await Catch.Exception(() => _storage.Store(_key));

    [Fact] void should_throw_the_failure() => _result.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_look_for_a_winner() => _collection.DidNotReceiveWithAnyArgs().FindAsync<DataProtectionKey>(default!, default, default);
}
