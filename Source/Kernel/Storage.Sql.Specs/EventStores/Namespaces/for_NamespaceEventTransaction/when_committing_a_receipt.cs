// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_committing_a_receipt : given.a_namespace_transaction
{
    int _result;
    bool _receipt;
    NamespaceMarker? _marker;

    async Task Establish() => await _markers.TryOpen(_incarnation, null);

    async Task Because()
    {
        _result = await _transaction.Execute(_context, WriteReceipt);
        _receipt = await HasReceipt();
        _marker = await _markers.Get();
    }

    [Fact] void should_commit_the_receipt() => _receipt.ShouldBeTrue();
    [Fact] void should_return_only_after_commit() => _result.ShouldEqual(42);
    [Fact] void should_write_the_marker_on_the_event_connection() => _marker!.Touch.ShouldEqual(1L);
    [Fact] void should_keep_the_same_lifetime() => _marker!.Incarnation.ShouldEqual(_incarnation);
}
