// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_closing_an_already_closed_identity : given.bson_storage
{
    bool _closed;

    void Establish() => _document = Marker(_identity, false, 7);

    async Task Because() => _closed = await new NamespaceMarkerStorage(_collection).TryClose(_identity);

    [Fact] void should_allow_idempotent_close_of_the_same_identity() => _closed.ShouldBeTrue();
    [Fact] void should_compare_the_exact_identity() => _filter["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldEqual(_identity.Value);
    [Fact] void should_not_require_the_marker_to_still_be_open() => _filter.Contains("isOpen").ShouldBeFalse();
    [Fact] void should_leave_the_marker_closed() => _update["$set"]["isOpen"].AsBoolean.ShouldBeFalse();
    [Fact] void should_conflict_with_in_flight_event_transactions() => _update["$inc"]["revision"].AsInt64.ShouldEqual(1L);
    [Fact] void should_not_upsert() => _upsert.ShouldBeFalse();
}
