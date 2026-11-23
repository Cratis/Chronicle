// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_activation_is_stale : given.a_namespace_transaction
{
    Exception? _failure;
    NamespaceMarker? _marker;

    async Task Establish()
    {
        await _markers.TryOpen(_incarnation, null);
        _context = _context with { ActivationIncarnation = NamespaceIncarnationId.New() };
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
        _marker = await _markers.Get();
    }

    [Fact] void should_require_reactivation() => _failure.ShouldBeOfExactType<NamespaceIncarnationRequiresReactivation>();
    [Fact] void should_not_invoke_the_body() => _called.ShouldBeFalse();
    [Fact] void should_rollback_the_fence_touch() => _marker!.Touch.ShouldEqual(0L);
}
