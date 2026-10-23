// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_opting_out : given.application_metadata_sharing
{
    void Because()
    {
        EventStoreForTesting.ShareApplicationMetadata = false;
        _other = new(null, _artifacts);
        _schema = SchemaFor(_store);
        _otherSchema = SchemaFor(_other);
    }

    [Fact] void should_not_give_the_store_metadata() => _other.Metadata.ShouldBeNull();
    [Fact] void should_preserve_the_schemas() => _schema.ToJson().ShouldEqual(_otherSchema.ToJson());
    [Fact] void should_discover_the_same_event_types() => _other.EventTypes.All.SequenceEqual(_store.EventTypes.All).ShouldBeTrue();
}
