// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_an_alternate_unit_receives_options;

public class with_an_implicit_scope : Specification
{
    Exception _exception;
    void Because() => _exception = Catch.Exception(() => ((IUnitOfWork)new given.a_legacy_unit()).AddImplicitScope(EventSequenceId.Log, "source", 7));
    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<UnitOfWorkImplicitScopesNotSupported>();
}
