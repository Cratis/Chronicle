// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_unchanged_metadata_through_a_batch : when_registering_unchanged_metadata
{
    protected override async Task<bool> RegisterUnchanged() => (await ((IEventTypesStorage)_storage).Register([new(_chain with { Generations = [_chain.Generations.First()], Migrations = [] }, EventTypeSource.Unknown)])).Any();
}
