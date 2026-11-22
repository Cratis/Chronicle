// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_the_caller_has_no_active_transaction : given.bson_storage
{
    Exception _error;

    void Establish() => _session.IsInTransaction.Returns(false);

    async Task Because() => _error = await Catch.Exception(() => new NamespaceMarkerStorage(_collection).TryFence(_session, _identity));

    [Fact] void should_refuse_sessionless_admission() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_issue_a_conflict_write() => _update.ShouldBeNull();
    [Fact] void should_not_recreate_a_marker() => _inserted.ShouldBeNull();
}
