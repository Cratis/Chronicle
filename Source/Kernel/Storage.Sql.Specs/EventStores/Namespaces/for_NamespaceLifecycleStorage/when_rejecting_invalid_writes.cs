// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceLifecycleStorage;

public class when_rejecting_invalid_writes : given.a_lifecycle_storage
{
    bool _wrongName;
    bool _wrongInitialRevision;
    bool _missingRevision;
    bool _unchangedRevision;
    bool _skippedRevision;
    NamespaceLifecycle? _result;

    async Task Establish() => await _storage.TryWrite(_name, null, Open());

    async Task Because()
    {
        _wrongName = await _storage.TryWrite("Other", null, Open());
        _wrongInitialRevision = await _storage.TryWrite(_name, null, Open() with { Revision = 2 });
        _missingRevision = await _storage.TryWrite("Missing", 1, Open() with { Namespace = "Missing", Revision = 2 });
        _unchangedRevision = await _storage.TryWrite(_name, 1, Open());
        _skippedRevision = await _storage.TryWrite(_name, 1, Open() with { Revision = 3 });
        _result = await CreateStorage().Get(_name);
    }

    [Fact] void should_reject_another_name() => _wrongName.ShouldBeFalse();
    [Fact] void should_require_revision_one_for_insert() => _wrongInitialRevision.ShouldBeFalse();
    [Fact] void should_never_upsert_a_revision_update() => _missingRevision.ShouldBeFalse();
    [Fact] void should_require_an_increment() => _unchangedRevision.ShouldBeFalse();
    [Fact] void should_reject_a_skipped_revision() => _skippedRevision.ShouldBeFalse();
    [Fact] void should_keep_the_original_state() => _result.ShouldEqual(Open());
}
