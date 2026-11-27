// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_retrying_the_same_open_identity : given.bson_storage
{
    bool _opened;

    void Establish() => _document = Marker(_identity, true, 3);

    async Task Because() => _opened = await new NamespaceMarkerStorage(_collection).TryOpen(_identity, null);

    [Fact] void should_acknowledge_the_existing_identity() => _opened.ShouldBeTrue();
    [Fact] void should_not_replace_the_marker() => _update.ShouldBeNull();
    [Fact] void should_not_insert_another_marker() => _inserted.ShouldBeNull();
}
