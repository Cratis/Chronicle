// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class with_an_unspecified_label : given.a_unit_with_implicit_scopes
{
    void Because() => _exception = Catch.Exception(() => Guard(source: EventSourceId.Unspecified));
    [Fact] void should_refuse_the_label() => _exception.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
}
