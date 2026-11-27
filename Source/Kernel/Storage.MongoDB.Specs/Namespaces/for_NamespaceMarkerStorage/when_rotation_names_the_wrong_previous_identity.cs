// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_rotation_names_the_wrong_previous_identity : given.bson_storage
{
    bool _opened;

    void Establish() => _document = Marker(NamespaceIncarnationId.New(), false, 7);

    async Task Because() => _opened = await new NamespaceMarkerStorage(_collection).TryOpen(_identity, NamespaceIncarnationId.New());

    [Fact] void should_refuse_rotation() => _opened.ShouldBeFalse();
    [Fact] void should_not_mutate_the_winning_marker() => _update.ShouldBeNull();
    [Fact] void should_not_insert_another_marker() => _inserted.ShouldBeNull();
}
