// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_rotating_a_closed_marker : given.bson_storage
{
    readonly NamespaceIncarnationId _previous = NamespaceIncarnationId.New();
    bool _opened;

    void Establish() => _document = Marker(_previous, false, 7);

    async Task Because() => _opened = await new NamespaceMarkerStorage(_collection).TryOpen(_identity, _previous);

    [Fact] void should_compare_the_exact_retired_identity() => _filter["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldEqual(_previous.Value);
    [Fact] void should_compare_the_closed_state() => _filter["isOpen"].AsBoolean.ShouldBeFalse();
    [Fact] void should_compare_the_prior_revision() => _filter["revision"].AsInt64.ShouldEqual(7L);
    [Fact] void should_install_a_new_uuid() => _update["$set"]["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldEqual(_identity.Value);
    [Fact] void should_not_upsert() => _upsert.ShouldBeFalse();
    [Fact] void should_acknowledge_the_rotation() => _opened.ShouldBeTrue();
}
