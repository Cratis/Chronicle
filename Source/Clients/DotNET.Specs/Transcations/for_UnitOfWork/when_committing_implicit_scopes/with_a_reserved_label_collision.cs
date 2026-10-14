// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_reserved_label_collision : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Enroll(new(1, EventStreamType: "items"), ConcurrencyScopeOrigin.Routing);
        Enroll(source: $"$keyguard:routing:{_sourceId.Value}");
    }
    Task Because() => Commit();
    [Fact] void should_refuse_to_overwrite_the_reserved_label() => _exception.ShouldBeOfExactType<ReservedConcurrencyScopeLabelCollision>();
}
