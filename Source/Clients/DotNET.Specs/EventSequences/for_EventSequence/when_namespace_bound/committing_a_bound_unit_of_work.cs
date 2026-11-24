// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class committing_a_bound_unit_of_work : given.a_namespace_bound_append
{
    async Task Because()
    {
        var unit = CreateUnitOfWork();
        unit.BindNamespace(_binding);
        unit.AddEvents(EventSequenceId.Log, [new("source", "event")], new Dictionary<EventSourceId, ConcurrencyScope> { ["source"] = new(42UL) });
        await unit.Commit();
    }

    [Fact] void should_send_the_original_identity() => _boundBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_preserve_the_scope() => _boundBatch.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(42UL);
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
