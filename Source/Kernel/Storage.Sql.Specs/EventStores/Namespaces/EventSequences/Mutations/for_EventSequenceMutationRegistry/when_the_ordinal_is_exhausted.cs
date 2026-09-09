// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_the_ordinal_is_exhausted : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _result;
    long _ordinal;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.EventSequenceMutationHeads.Add(new()
        {
            EventSequenceId = _target.Display,
            LastAssignedOrdinal = long.MaxValue,
            Coverage = EventSequenceMutationCoverage.Untracked
        });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        _result = await _registry.Begin(_request, _proposedTarget);
        await using var context = CreateContext();
        _ordinal = (await context.EventSequenceMutationHeads.SingleAsync()).LastAssignedOrdinal.Value;
    }

    [Fact] void should_fail_closed() => _result.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Corrupt);
    [Fact] void should_not_wrap_or_change_the_counter() => _ordinal.ShouldEqual(long.MaxValue);
}
