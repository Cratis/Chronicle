// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class with_before_first_and_an_actual_boundary : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard(7);
        Guard(EventSequenceNumber.BeforeFirst);
        Guard(3);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_treat_before_first_as_the_smallest() => ScopeFor(_sourceId).ExpectsNoMatchingEvent.ShouldBeTrue();
}
