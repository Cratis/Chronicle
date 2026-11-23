// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_marker_is_closed : given.a_namespace_transaction
{
    Exception? _failure;
    bool _receipt;
    NamespaceMarker? _marker;

    async Task Establish()
    {
        await _markers.TryOpen(_incarnation, null);
        await _markers.TryClose(_incarnation);
        _context = _context with { ActivationIncarnation = null };
    }

    async Task Because()
    {
        try
        {
            await _transaction.Execute(_context, WriteReceipt);
        }
        catch (Exception error)
        {
            _failure = error;
        }
        _receipt = await HasReceipt();
        _marker = await _markers.Get();
    }

    [Fact] void should_reject_before_comparing_activation_state() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_not_invoke_the_body() => _called.ShouldBeFalse();
    [Fact] void should_have_no_receipt() => _receipt.ShouldBeFalse();
    [Fact] void should_not_touch_a_closed_marker() => _marker!.Touch.ShouldEqual(1L);
}
