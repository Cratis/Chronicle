// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_an_event_store_whose_name_cannot_be_resolved : given.a_storage
{
    Exception _exception;

    void Establish() => _clusterStorage.RejectCreation = true;

    void Because() => _exception = Catch.Exception(() => _storage.GetEventStore("admin"));

    [Fact] void should_fail() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_register_the_name() => _clusterStorage.SavedEventStores.ShouldBeEmpty();
}
