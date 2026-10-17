// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_an_unsupported_implementation : Specification
{
    Exception _error;

    void Because()
    {
        IUnitOfWork unit = new for_UnitOfWork.given.a_legacy_unit();
        _error = Catch.Exception(() => unit.RefuseImmediateAppends());
    }

    [Fact] void should_fail_explicitly_instead_of_silently_allowing_appends() => _error.ShouldBeOfExactType<UnitOfWorkImmediateAppendRefusalNotSupported>();
}
