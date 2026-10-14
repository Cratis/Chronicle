// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_an_alternate_unit_receives_options;

public class with_a_batch : Specification
{
    readonly given.a_legacy_unit _unit = new();
    void Because() => ((IUnitOfWork)_unit).AddEvents(EventSequenceId.Log, [new EventForEventSourceId("source", "event")], [new("source", new(7))], ConcurrencyScopeOrigin.Routing);
    [Fact] void should_use_the_existing_batch_member() => _unit.BatchUsed.ShouldBeTrue();
    [Fact] void should_keep_the_scope() => _unit.Scope!.SequenceNumber.Value.ShouldEqual(7UL);
}
