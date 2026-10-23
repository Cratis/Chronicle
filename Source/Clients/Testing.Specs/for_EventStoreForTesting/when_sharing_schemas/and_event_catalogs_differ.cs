// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_event_catalogs_differ : given.schema_sharing
{
    void Because() => _other = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(OtherEvent)] });

    [Fact] void should_share_structural_generation_across_event_catalogs() => GeneratorFor(_store).ShouldBeSame(GeneratorFor(_other));
    [Fact] void should_not_add_the_other_catalogs_event_to_the_first_store() => _store.EventTypes.HasFor(typeof(OtherEvent)).ShouldBeFalse();
    [Fact] void should_not_add_the_first_catalogs_event_to_the_other_store() => _other.EventTypes.HasFor(typeof(StoreIdentityRecorded)).ShouldBeFalse();

    [EventType]
    public record OtherEvent(int Count);
}
