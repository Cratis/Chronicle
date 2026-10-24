// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_storing_a_data_protection_key_and_no_winner_can_be_found : a_data_protection_key_collection
{
    Exception _result;

    void Establish()
    {
        InsertFailsWith(duplicate_keys.Duplicate());
        WinnerIs();
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.Store(_key));

    [Fact] void should_keep_the_duplicate_key_error() => _result.ShouldBeOfExactType<MongoWriteException>();
}
