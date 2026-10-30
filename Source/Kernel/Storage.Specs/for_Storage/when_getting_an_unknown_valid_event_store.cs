// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_an_unknown_valid_event_store : given.a_storage
{
    IEventStoreStorage _result;

    void Because() => _result = _storage.GetEventStore("NewStore");

    [Fact] void should_create_storage() => _clusterStorage.CreatedStorages.ShouldContain(_result);
    [Fact] async Task should_register_the_store() => (await _storage.HasEventStore("NewStore")).ShouldBeTrue();
}
