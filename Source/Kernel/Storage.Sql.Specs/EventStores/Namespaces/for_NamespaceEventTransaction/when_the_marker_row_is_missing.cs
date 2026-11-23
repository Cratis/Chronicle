// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_marker_row_is_missing : given.a_namespace_transaction
{
    Exception? _failure;
    NamespaceMarker? _marker;

    async Task Establish() => await _markers.Get();

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

    [Fact] void should_reject_the_transaction() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_not_invoke_the_body() => _called.ShouldBeFalse();
    [Fact] void should_never_upsert_a_marker() => _marker.ShouldBeNull();
}
