// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_a_contributed_source_less_read : given.an_enforcing_unit
{
    void Establish() => _contribution = _contribution with { Label = "numbering-check", Scope = new ConcurrencyScope(100, EventTypes: [_changed]) };

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_refuse_a_source_less_acquisition() => _error.ShouldBeOfExactType<SourceLessReadNotSupported>();
}
