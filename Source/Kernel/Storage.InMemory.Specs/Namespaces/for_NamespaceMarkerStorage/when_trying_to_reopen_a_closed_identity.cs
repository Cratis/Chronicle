// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces.for_NamespaceMarkerStorage;

public class when_trying_to_reopen_a_closed_identity : Specification
{
    NamespaceMarkerStorage _storage;
    NamespaceIncarnationId _incarnation;
    NamespaceMarker _result;
    bool _opened;

    async Task Establish()
    {
        _storage = new();
        _incarnation = NamespaceIncarnationId.New();
        await _storage.TryOpen(_incarnation, null);
        await _storage.TryClose(_incarnation);
    }

    async Task Because()
    {
        _opened = await _storage.TryOpen(_incarnation, null);
        _result = await _storage.Get();
    }

    [Fact] void should_never_reopen_the_closed_identity() => _opened.ShouldBeFalse();
    [Fact] void should_preserve_the_closed_marker() => _result.IsOpen.ShouldBeFalse();
    [Fact] void should_preserve_the_incarnation() => _result.Incarnation.ShouldEqual(_incarnation);
}
