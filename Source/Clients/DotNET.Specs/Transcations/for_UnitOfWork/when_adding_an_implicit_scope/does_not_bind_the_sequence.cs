// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class does_not_bind_the_sequence : given.a_unit_with_implicit_scopes
{
    void Establish() => Guard(sequence: "other-sequence");
    void Because() => _exception = Catch.Exception(() => _enrollment.AddEvents(EventSequenceId.Log, [], []));
    [Fact] void should_allow_binding_to_another_sequence() => _exception.ShouldBeNull();
}
