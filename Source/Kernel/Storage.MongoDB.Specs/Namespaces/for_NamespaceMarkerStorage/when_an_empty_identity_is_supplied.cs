// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_an_empty_identity_is_supplied : given.bson_storage
{
    bool _opened;
    bool _rotated;
    bool _closed;
    bool _fenced;

    async Task Because()
    {
        var storage = new NamespaceMarkerStorage(_collection);
        _opened = await storage.TryOpen(NamespaceIncarnationId.NotSet, null);
        _rotated = await storage.TryOpen(_identity, NamespaceIncarnationId.NotSet);
        _closed = await storage.TryClose(NamespaceIncarnationId.NotSet);
        _fenced = await storage.TryFence(_session, NamespaceIncarnationId.NotSet);
    }

    [Fact] void should_not_open_an_empty_identity() => _opened.ShouldBeFalse();
    [Fact] void should_not_treat_empty_as_an_absent_previous_identity() => _rotated.ShouldBeFalse();
    [Fact] void should_not_close_an_empty_identity() => _closed.ShouldBeFalse();
    [Fact] void should_not_fence_an_empty_identity() => _fenced.ShouldBeFalse();
    [Fact] void should_not_insert_a_marker() => _inserted.ShouldBeNull();
    [Fact] void should_not_modify_a_marker() => _update.ShouldBeNull();
}
