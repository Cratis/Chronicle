// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_Storage.when_getting_event_store;

public class and_name_is_not_set : given.a_storage
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _storage.GetEventStore(EventStoreName.NotSet));

    [Fact] void should_throw_invalid_event_store_name() => _exception.ShouldBeOfExactType<InvalidEventStoreName>();
}
