// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class with_a_non_concrete_boundary : given.a_unit_with_implicit_scopes
{
    Exception _unavailable;
    void Because()
    {
        _exception = Catch.Exception(() => Guard(EventSequenceNumber.Max));
        _unavailable = Catch.Exception(() => Guard(EventSequenceNumber.Unavailable));
    }
    [Fact] void should_refuse_not_set() => _exception.ShouldBeOfExactType<ImplicitScopeBoundaryMustBeConcrete>();
    [Fact] void should_refuse_unavailable() => _unavailable.ShouldBeOfExactType<ImplicitScopeBoundaryMustBeConcrete>();
}
