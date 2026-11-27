// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_creating_control_state : given.bson_storage
{
    NamespaceLifecycle _state;
    bool _won;

    void Establish() => _state = new(_name, _identity, DateTimeOffset.UtcNow);

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, null, _state);

    [Fact] void should_acknowledge_the_insert() => _won.ShouldBeTrue();
    [Fact] void should_retain_the_complete_state() => NamespaceLifecycleDocument.Decode(_inserted).ShouldEqual(_state);
    [Fact] void should_not_use_a_replacement_upsert() => _replacement.ShouldBeNull();
    [Fact] void should_require_majority_durability() => _collection.Received(1).WithWriteConcern(WriteConcern.WMajority);
}
