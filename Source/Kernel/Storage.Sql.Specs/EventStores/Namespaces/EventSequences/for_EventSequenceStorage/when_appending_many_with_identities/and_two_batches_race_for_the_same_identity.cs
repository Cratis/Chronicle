// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_two_batches_race_for_the_same_identity : given.a_keyed_batch_storage
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>[] _results;
    async Task Because()
    {
        var attempts = await Task.WhenAll(Task.Run(() => Attempt(0)), Task.Run(() => Attempt(10)));
        _results = new Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>[2];
        for (var index = 0; index < attempts.Length; index++)
        {
            // MongoDB may prove an overlapping transaction did not commit before its winner is visible.
            // Both contenders have finished here, so one bounded settlement attempt must find the winner.
            _results[index] = attempts[index] ?? await _storage.AppendManyWithIdentities([_event with { SequenceNumber = (ulong)(index * 10), Identity = _publication }]);
        }
    }
    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>?> Attempt(ulong number)
    {
        try
        {
            return await _storage.AppendManyWithIdentities([_event with { SequenceNumber = number, Identity = _publication }, _event with { SequenceNumber = number + 1 }]);
        }
        catch (EventsNotCommitted)
        {
            return null;
        }
    }
    [Fact] void should_commit_only_one_batch() => _results.Count(result => result.IsSuccess).ShouldEqual(1);
    [Fact] void should_report_the_winning_receipt_to_the_loser() => _results.Single(result => !result.IsSuccess).AsT1.AsT1.Receipts[_publication.Id].SequenceNumber.ShouldEqual(_results.Single(result => result.IsSuccess).AsT0.First().Context.SequenceNumber);
    [Fact] async Task should_not_commit_any_events_from_the_losing_batch() => (await _storage.GetCount()).Value.ShouldEqual(2UL);
}
