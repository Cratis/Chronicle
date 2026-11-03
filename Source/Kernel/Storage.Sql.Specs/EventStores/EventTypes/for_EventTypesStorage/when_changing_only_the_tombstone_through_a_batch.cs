// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_changing_only_the_tombstone_through_a_batch : when_changing_only_the_tombstone
{
    protected override Task<bool> RegisterMetadata() => RegisterMetadataBatch(new(_chain with { Tombstone = true }, EventTypeSource.Unknown));
}
