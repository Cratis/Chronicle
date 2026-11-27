// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_identity_is_empty : given.a_namespace_transaction
{
    void Establish() => _context = new(new(Guid.Empty), _activation.Token);
    Task Because() => ExecuteCatching();

    [Fact] void should_reject_the_unbound_identity() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_not_start_an_attempt() => _attempts.ShouldEqual(0);
    [Fact] void should_not_write() => _steps.ShouldBeEmpty();
}
