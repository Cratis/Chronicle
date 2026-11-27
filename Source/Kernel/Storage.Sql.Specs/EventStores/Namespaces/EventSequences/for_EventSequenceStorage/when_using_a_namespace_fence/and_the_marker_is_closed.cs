// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_the_marker_is_closed : given.a_fenced_storage
{
    readonly List<Exception> _errors = [];
    ulong _count;

    async Task Establish() => (await _markers.TryClose(_incarnation)).ShouldBeTrue();

    async Task Because()
    {
        foreach (var operation in FencedOperations()) _errors.Add(await Catch.Exception(operation));
        _count = (await _storage.GetCount()).Value;
    }

    [Fact] void should_reject_every_fenced_operation() => _errors.ShouldEachConformTo(error => error is NamespaceCommitRejected);
    [Fact] void should_append_nothing() => _count.ShouldEqual(0UL);
    [Fact] async Task should_not_write_a_receipt() => (await _storage.GetEffectReceipt(_receiptId)).ShouldBeNull();
}
