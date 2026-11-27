// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_no_transaction_is_active : given.a_namespace_transaction
{
    Exception? _failure;
    NamespaceMarker? _marker;

    async Task Establish() => await _markers.TryOpen(_incarnation, null);

    async Task Because()
    {
        await using var scope = await _database.Namespace("fenced-store", "fenced-namespace");
        try
        {
            await NamespaceMarkerStorage.TryFence(scope.DbContext, _incarnation, CancellationToken.None);
        }
        catch (Exception error)
        {
            _failure = error;
        }
        _marker = await _markers.Get();
    }

    [Fact] void should_reject_a_sessionless_fence() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_touch_the_marker() => _marker!.Touch.ShouldEqual(0L);
}
