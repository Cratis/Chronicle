// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_changing_only_the_owner : when_changing_only_metadata
{
    protected override Task<bool> RegisterMetadata() => _storage.Register(new(_chain.Id, 1), _chain.Generations.First().Schema, owner: EventTypeOwner.Server, source: EventTypeSource.Unknown);

    [Fact] void should_store_the_owner() => _after.Owner.ShouldEqual(EventTypeOwner.Server);
}
