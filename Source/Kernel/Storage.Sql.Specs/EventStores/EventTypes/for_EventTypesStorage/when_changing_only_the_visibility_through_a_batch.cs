// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_changing_only_the_visibility_through_a_batch : when_changing_only_the_visibility
{
    protected override Task<bool> RegisterMetadata() => RegisterMetadataBatch(new(_chain, EventTypeSource.Unknown, EventTypeVisibility.Private));
}
