// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_storing_a_data_protection_key_that_an_equivalent_winner_inserted : a_data_protection_key_collection
{
    Exception _result;

    void Establish()
    {
        InsertFailsWith(duplicate_keys.Duplicate());
        WinnerIs(_key with { });
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.Store(_key));

    [Fact] void should_converge_without_failing() => _result.ShouldBeNull();
}
