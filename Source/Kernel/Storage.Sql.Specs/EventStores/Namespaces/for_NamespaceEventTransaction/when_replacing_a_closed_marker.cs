// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_replacing_a_closed_marker : given.a_namespace_transaction
{
    readonly NamespaceIncarnationId _replacement = NamespaceIncarnationId.New();
    bool _open;
    bool _idempotent;
    bool _wrongPrevious;
    bool _replaced;
    bool _staleClose;
    bool _empty;
    NamespaceMarker? _marker;

    async Task Because()
    {
        _open = await _markers.TryOpen(_incarnation, null);
        _idempotent = await _markers.TryOpen(_incarnation, null);
        await _markers.TryClose(_incarnation);
        _wrongPrevious = await _markers.TryOpen(_replacement, NamespaceIncarnationId.New());
        _replaced = await _markers.TryOpen(_replacement, _incarnation);
        _staleClose = await _markers.TryClose(_incarnation);
        _empty = await _markers.TryOpen(NamespaceIncarnationId.NotSet, _replacement);
        _marker = await _markers.Get();
    }

    [Fact] void should_insert_an_absent_marker() => _open.ShouldBeTrue();
    [Fact] void should_accept_an_already_open_same_incarnation() => _idempotent.ShouldBeTrue();
    [Fact] void should_reject_an_unrelated_closed_incarnation() => _wrongPrevious.ShouldBeFalse();
    [Fact] void should_replace_the_exact_previous_incarnation() => _replaced.ShouldBeTrue();
    [Fact] void should_never_close_a_replacement_with_the_old_incarnation() => _staleClose.ShouldBeFalse();
    [Fact] void should_reject_an_empty_identity() => _empty.ShouldBeFalse();
    [Fact] void should_preserve_conflict_history() => _marker!.Touch.ShouldEqual(2L);
    [Fact] void should_leave_the_new_incarnation_open() => _marker.ShouldEqual(new NamespaceMarker(_replacement, true, 2));
}
