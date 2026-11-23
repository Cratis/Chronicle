// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceLifecycleStorage;

public class when_a_stale_revision_replaces_a_tombstone : given.a_lifecycle_storage
{
    NamespaceLifecycle _tombstone;
    NamespaceLifecycle? _result;
    bool _advanced;
    bool _staleWrite;
    bool _reinsert;

    async Task Establish()
    {
        await _storage.TryWrite(_name, null, Open());
        _tombstone = Open() with { Revision = 2, Status = NamespaceLifecycleStatus.Removed };
    }

    async Task Because()
    {
        _advanced = await _storage.TryWrite("ORDERS", 1, _tombstone);
        _staleWrite = await CreateStorage().TryWrite(_name, 1, Open() with { Revision = 2 });
        _reinsert = await CreateStorage().TryWrite(_name, null, Open());
        _result = await CreateStorage().Get(_name);
    }

    [Fact] void should_advance_the_exact_revision() => _advanced.ShouldBeTrue();
    [Fact] void should_reject_the_stale_revision() => _staleWrite.ShouldBeFalse();
    [Fact] void should_not_reinitialize_a_removed_name() => _reinsert.ShouldBeFalse();
    [Fact] void should_retain_the_tombstone() => _result.ShouldEqual(_tombstone);
}
