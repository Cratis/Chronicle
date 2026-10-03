// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class equivalent_guards_where_the_tail_moves : given.a_definition_backed_batch
{
    /// <summary>
    /// Each strategy read answers a different tail, yet the guards cover the same predicate.
    /// </summary>
    async Task Because() => await Append([Item("2025-01"), Item("2025-01")]);

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_send_one_scope() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
}
