// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

/// <summary>
/// Covers Begin registration-conflict outcomes: resuming an exact request, a definition
/// conflict on a mismatched resume, a busy target, and reserving an independent target.
/// </summary>
/// <remarks>
/// This does not cover the in-memory reference's "same id, different target" scenario
/// (a manually mismatched request whose <c>Id</c> was computed for a different target sequence
/// than the one it now carries). The in-memory registry catches that case only because it keeps a
/// second, global-by-id index that survives independently of any one target's head row. The SQL
/// schema intentionally has no such table (two tables only: heads keyed by event sequence, history
/// keyed by event sequence + ordinal with a unique index on MutationId) - a request's id is
/// re-verified as deterministic for its *own* target sequence on every fresh reservation instead
/// (<see cref="EventSequenceMutationValidator.ValidateDeterministicId"/>), which is a strictly
/// stronger check for that case: a request presenting an id that does not match its own target
/// is rejected as Invalid rather than silently reinterpreted against a stale registration.
/// </remarks>
public class when_resolving_begin_registration_conflicts : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _resumed;
    EventSequenceMutationBeginResult _definitionConflict;
    EventSequenceMutationBeginResult _busy;
    EventSequenceMutationBeginResult _independent;

    async Task Because()
    {
        await _registry.Begin(_request, _proposedTarget);
        _resumed = await _registry.Begin(_request, new(100UL, 101UL, 1UL));
        _definitionConflict = await _registry.Begin(_request with { Command = new("different", "different-hash") }, _proposedTarget);
        _busy = await _registry.Begin(Request(_target, originSequenceNumber: 43), _proposedTarget);
        _independent = await _registry.Begin(Request(Identity("independent-target")), _proposedTarget);
    }

    [Fact] void should_resume_the_exact_request() => _resumed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);
    [Fact] void should_ignore_a_new_target_for_the_exact_request() => _resumed.Active!.Target.ShouldEqual(_proposedTarget);
    [Fact] void should_reject_a_nonexact_request() => _definitionConflict.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_report_the_exact_busy_outcome() => (_busy.Outcome == EventSequenceMutationBeginOutcome.MutationAlreadyInProgress && _busy.Error == EventSequenceMutationRegistryError.MutationAlreadyInProgress).ShouldBeTrue();
    [Fact] void should_report_only_the_active_id_when_the_target_is_busy() => _busy.ConflictingMutationId.ShouldEqual(_request.Id);
    [Fact] void should_reserve_an_independent_target() => _independent.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
}
