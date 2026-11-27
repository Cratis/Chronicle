// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_the_lifetime_is_replaced : given.a_fenced_storage
{
    readonly List<Exception> _errors = [];

    async Task Establish()
    {
        await _fenced.AppendPublication(_context, _publication, _event);
        await _fenced.AppendManyWithEffectReceipt(_context, [], Receipt(null));
        (await _markers.TryClose(_incarnation)).ShouldBeTrue();
        (await _markers.TryOpen(NamespaceIncarnationId.New(), _incarnation)).ShouldBeTrue();
    }

    async Task Because()
    {
        foreach (var operation in FencedOperations()) _errors.Add(await Catch.Exception(operation));
    }

    [Fact] void should_reject_even_preexisting_receipts() => _errors.ShouldEachConformTo(error => error is NamespaceCommitRejected);
    [Fact] async Task should_not_append_again() => (await _storage.GetCount()).Value.ShouldEqual(1UL);
}
