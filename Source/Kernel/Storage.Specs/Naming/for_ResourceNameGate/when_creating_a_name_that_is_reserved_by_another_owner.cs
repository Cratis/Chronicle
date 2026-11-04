// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_a_name_that_is_reserved_by_another_owner : given.a_gate
{
    ResourceNameConflict _conflict;

    void Establish() => _reservations.Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(false);

    void Because() => _conflict = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Store("orders")).GetAwaiter().GetResult());

    [Fact] void should_refuse_with_an_overlap() => _conflict.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
}
