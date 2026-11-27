// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_fencing_an_active_event_transaction : given.bson_storage
{
    bool _admitted;

    async Task Because() => _admitted = await new NamespaceMarkerStorage(_collection).TryFence(_session, _identity);

    [Fact] void should_write_on_the_callers_session() => _usedSession.ShouldEqual(_session);
    [Fact] void should_compare_the_exact_incarnation() => _filter["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldEqual(_identity.Value);
    [Fact] void should_require_an_open_marker() => _filter["isOpen"].AsBoolean.ShouldBeTrue();
    [Fact] void should_change_a_real_conflict_revision() => _update["$inc"]["revision"].AsInt64.ShouldEqual(1L);
    [Fact] void should_never_upsert() => _upsert.ShouldBeFalse();
    [Fact] void should_not_substitute_a_sessionless_check() => _reads.ShouldEqual(0);
    [Fact] void should_acknowledge_the_fence() => _admitted.ShouldBeTrue();
}
