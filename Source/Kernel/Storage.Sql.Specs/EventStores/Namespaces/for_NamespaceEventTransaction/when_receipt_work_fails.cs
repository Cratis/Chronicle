// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_receipt_work_fails : given.a_namespace_transaction
{
    readonly InvalidOperationException _expected = new("receipt body failed");
    Exception? _failure;
    bool _receipt;
    NamespaceMarker? _marker;

    async Task Establish() => await _markers.TryOpen(_incarnation, null);

    async Task Because()
    {
        try
        {
            await _transaction.Execute<int>(_context, async (database, cancellationToken) =>
            {
                await WriteReceipt(database, cancellationToken);
                throw _expected;
            });
        }
        catch (Exception error)
        {
            _failure = error;
        }
        _receipt = await HasReceipt();
        _marker = await _markers.Get();
    }

    [Fact] void should_propagate_the_exact_failure() => _failure.ShouldEqual(_expected);
    [Fact] void should_rollback_the_receipt() => _receipt.ShouldBeFalse();
    [Fact] void should_rollback_the_marker_touch() => _marker!.Touch.ShouldEqual(0L);
}
