// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_a_mutation;

/// <summary>
/// Sixteen callers racing <see cref="IEventSequenceMutationRegistry.Begin"/> with the exact same request against a
/// real MongoDB must settle on exactly one winning reservation, with every other caller resuming it - proving the
/// atomic upsert-if-absent-or-null compare-and-swap serializes concurrent reservations correctly, matching the
/// in-memory registry's own concurrency proof.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_racing_concurrently(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    const int CallerCount = 16;

    EventSequenceMutationBeginResult[] _results = default!;

    async Task Because() =>
        _results = await Race(Enumerable.Range(0, CallerCount)
            .Select(_ => (Func<Task<EventSequenceMutationBeginResult>>)(() => Registry.Begin(Request, ProposedTarget))));

    [Fact] void should_have_exactly_one_winner() => _results.Count(_ => _.Outcome == EventSequenceMutationBeginOutcome.Reserved).ShouldEqual(1);
    [Fact] void should_resume_every_other_caller() => _results.Count(_ => _.Outcome == EventSequenceMutationBeginOutcome.Resumed).ShouldEqual(CallerCount - 1);
    [Fact] void should_make_every_caller_observe_the_same_ordinal() => _results.Select(_ => _.Active!.Ordinal).Distinct().Count().ShouldEqual(1);
    [Fact] void should_assign_the_first_ordinal() => _results.All(_ => _.Active!.Ordinal == EventSequenceMutationOrdinal.First).ShouldBeTrue();

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
