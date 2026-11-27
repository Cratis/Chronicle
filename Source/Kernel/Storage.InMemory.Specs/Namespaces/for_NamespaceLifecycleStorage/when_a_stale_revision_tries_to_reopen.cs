// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces.for_NamespaceLifecycleStorage;

public class when_a_stale_revision_tries_to_reopen : Specification
{
    NamespaceLifecycleStorage _storage;
    NamespaceLifecycle _open;
    NamespaceLifecycle _closing;
    NamespaceLifecycle _result;
    bool _written;

    async Task Establish()
    {
        _storage = new();
        _open = NamespaceLifecycle.Open("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch);
        await _storage.TryWrite("orders", null, _open);
        _closing = _open.BeginRemoval(Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        await _storage.TryWrite("orders", _open.Revision, _closing);
    }

    async Task Because()
    {
        _written = await _storage.TryWrite("ORDERS", _open.Revision, _open with { Revision = _open.Revision + 1 });
        _result = await _storage.Get("orders");
    }

    [Fact] void should_reject_the_stale_completion() => _written.ShouldBeFalse();
    [Fact] void should_keep_the_closing_operation() => _result.ShouldEqual(_closing);
}
