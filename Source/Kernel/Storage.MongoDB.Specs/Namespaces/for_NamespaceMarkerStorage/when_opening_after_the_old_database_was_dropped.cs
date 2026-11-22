// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_opening_after_the_old_database_was_dropped : given.bson_storage
{
    bool _opened;

    async Task Because() => _opened = await new NamespaceMarkerStorage(_collection).TryOpen(_identity, NamespaceIncarnationId.New());

    [Fact] void should_install_the_new_identity() => _inserted["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldEqual(_identity.Value);
    [Fact] void should_use_the_singleton_key() => _inserted["_id"].AsInt32.ShouldEqual(1);
    [Fact] void should_open_only_after_inserting_the_marker() => _opened.ShouldBeTrue();
    [Fact] void should_require_a_majority_acknowledgement() => _collection.Received(1).WithWriteConcern(WriteConcern.WMajority);
}
