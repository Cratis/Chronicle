// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_beginning_concurrently : given.a_mutation_registry
{
    EventSequenceMutationBeginResult[] _sameRequest;
    EventSequenceMutationBeginResult[] _sameTarget;
    EventSequenceMutationBeginResult[] _independentTargets;

    async Task Because()
    {
        _sameRequest = await Race(Enumerable.Range(0, 16)
            .Select(index => (Func<Task<EventSequenceMutationBeginResult>>)(() =>
                _registry.Begin(_request, new((ulong)index, (ulong)index + 1, 1)))));

        var busyRegistry = Registry();
        _sameTarget = await Race(Enumerable.Range(0, 16)
            .Select(index => (Func<Task<EventSequenceMutationBeginResult>>)(() =>
                busyRegistry.Begin(Request(_target, originSequenceNumber: (ulong)index + 100), _proposedTarget))));

        var independentRegistry = Registry();
        _independentTargets = await Race(Enumerable.Range(0, 16)
            .Select(index => (Func<Task<EventSequenceMutationBeginResult>>)(() =>
            {
                var target = Identity($"target-{index:D2}");
                return independentRegistry.Begin(Request(target), _proposedTarget);
            })));
    }

    [Fact] void should_have_exactly_one_first_registration_winner() => _sameRequest.Count(_ => _.Outcome == EventSequenceMutationBeginOutcome.Reserved).ShouldEqual(1);
    [Fact] void should_resume_every_other_exact_request() => _sameRequest.Count(_ => _.Outcome == EventSequenceMutationBeginOutcome.Resumed).ShouldEqual(15);
    [Fact] void should_make_every_exact_caller_use_the_winning_frozen_target() => _sameRequest.Select(_ => _.Active!.Target).Distinct().Count().ShouldEqual(1);
    [Fact] void should_make_every_exact_caller_use_one_ordinal_and_version() => _sameRequest.All(_ => _.Active!.Ordinal.Value == 1 && _.Active.StateVersion.Value == 1).ShouldBeTrue();
    [Fact] void should_have_exactly_one_winner_for_different_ids_on_one_target() => _sameTarget.Count(_ => _.Outcome == EventSequenceMutationBeginOutcome.Reserved).ShouldEqual(1);
    [Fact] void should_report_the_exact_busy_outcome_for_every_loser() => _sameTarget.Where(_ => _.Outcome != EventSequenceMutationBeginOutcome.Reserved).All(_ => _.Outcome == EventSequenceMutationBeginOutcome.MutationAlreadyInProgress && _.Error == EventSequenceMutationRegistryError.MutationAlreadyInProgress).ShouldBeTrue();
    [Fact] void should_name_only_the_winning_id_for_every_busy_loser()
    {
        var winnerId = _sameTarget.Single(_ => _.Outcome == EventSequenceMutationBeginOutcome.Reserved).Active!.Id;
        _sameTarget.Where(_ => _.Outcome != EventSequenceMutationBeginOutcome.Reserved).All(_ => _.ConflictingMutationId == winnerId).ShouldBeTrue();
    }
    [Fact] void should_allow_every_independent_target_to_win() => _independentTargets.All(_ => _.Outcome == EventSequenceMutationBeginOutcome.Reserved).ShouldBeTrue();

    static async Task<EventSequenceMutationBeginResult[]> Race(IEnumerable<Func<Task<EventSequenceMutationBeginResult>>> operations)
    {
        var operationArray = operations.ToArray();
        var remaining = operationArray.Length;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = operationArray.Select(operation => Task.Run(async () =>
        {
            if (Interlocked.Decrement(ref remaining) == 0)
            {
                ready.SetResult();
            }

            await start.Task;
            return await operation();
        })).ToArray();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        start.SetResult();
        return await Task.WhenAll(tasks);
    }
}
