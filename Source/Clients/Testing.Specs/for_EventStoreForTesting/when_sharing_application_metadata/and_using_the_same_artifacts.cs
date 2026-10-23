// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_using_the_same_artifacts : given.application_metadata_sharing
{
    void Because()
    {
        _other = new(null, _artifacts);
        _schema = SchemaFor(_store);
        _otherSchema = SchemaFor(_other);
    }

    [Fact] void should_share_the_metadata() => _store.Metadata.ShouldBeSame(_other.Metadata);
    [Fact] void should_have_metadata() => _store.Metadata.ShouldNotBeNull();
    [Fact] void should_discover_the_same_event_types() => _other.EventTypes.All.SequenceEqual(_store.EventTypes.All).ShouldBeTrue();
    [Fact] void should_not_share_the_schema_instances() => _schema.ShouldNotBeSame(_otherSchema);
    [Fact] void should_preserve_the_schemas() => _schema.ToJson().ShouldEqual(_otherSchema.ToJson());
    [Fact] void should_resolve_the_same_clr_type() => _other.EventTypes.GetClrTypeFor(_other.EventTypes.GetEventTypeFor(typeof(StoreIdentityRecorded)).Id).ShouldEqual(typeof(StoreIdentityRecorded));
}
