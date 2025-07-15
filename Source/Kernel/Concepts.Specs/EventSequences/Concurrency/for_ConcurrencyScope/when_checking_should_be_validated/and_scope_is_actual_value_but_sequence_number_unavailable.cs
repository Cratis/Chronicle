// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScope.when_checking_should_be_validated;

public class and_scope_is_actual_value_but_sequence_number_unavailable : Specification
{
    ConcurrencyScope _scope;

    void Establish() => _scope = new ConcurrencyScope(EventSequenceNumber.Unavailable, true, null, null, null, null);

    [Fact]
    void should_not_validate() => _scope.ShouldBeValidated.ShouldBeFalse();
}
