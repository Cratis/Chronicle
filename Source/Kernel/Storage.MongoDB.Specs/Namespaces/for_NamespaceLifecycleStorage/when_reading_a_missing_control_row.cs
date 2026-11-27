// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_reading_a_missing_control_row : given.bson_storage
{
    NamespaceLifecycle _state;

    async Task Because() => _state = await new NamespaceLifecycleStorage(_collection).Get(_name);

    [Fact] void should_not_invent_an_open_lifetime() => _state.ShouldBeNull();
    [Fact] void should_read_from_the_primary() => _collection.Received(1).WithReadPreference(ReadPreference.Primary);
    [Fact] void should_require_majority_visibility() => _collection.Received(1).WithReadConcern(ReadConcern.Majority);
    [Fact] void should_not_insert_on_read() => _inserted.ShouldBeNull();
}
