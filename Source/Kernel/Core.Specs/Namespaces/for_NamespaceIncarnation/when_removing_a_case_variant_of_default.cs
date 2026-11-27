// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_removing_a_case_variant_of_default : given.an_owner
{
    Exception _error;

    /// <inheritdoc/>
    protected override EventStoreNamespaceName OwnerNamespace => EventStoreNamespaceName.Default.Value.ToUpperInvariant();

    async Task Because() => _error = await Catch.Exception(_owner.Remove);

    [Fact] void should_refuse_removal() => _error.ShouldBeOfExactType<NamespaceCannotBeRemoved>();
    [Fact] void should_not_write_pending_work() => _writes.Count.ShouldEqual(0);
    [Fact] async Task should_not_close_the_marker() => await _marker.DidNotReceive().TryClose(Arg.Any<NamespaceIncarnationId>());
}
