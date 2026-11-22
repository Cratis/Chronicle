// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_the_outcome_is_ambiguous : given.a_bulk_write_that_fails
{
    readonly List<(Exception Original, Exception? Surfaced)> _errors = [];
    Func<WriteModel<ObserverPartitionCounts>[], Exception>[] _failures;

    void Establish() => _failures =
    [
        models => new MongoConnectionException(_connectionId, "connection lost"),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models), [], ConcernError(), []),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models[..2]), [WriteError(1)], ConcernError(), models[2..]),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, new BulkWriteResult<ObserverPartitionCounts>.Unacknowledged(models.Length, models[..2]), [WriteError(1)], null, models[2..]),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models), [WriteError(0), WriteError(1)], null, []),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models), [WriteError(-1)], null, []),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models), [WriteError(models.Length)], null, []),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models[..1]), [WriteError(1)], null, models[2..]),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models[..2].Reverse()), [WriteError(1)], null, models[2..]),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models[..2]), [WriteError(1)], null, []),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models[..1]), [WriteError(0)], null, models[..2]),
        models => new MongoBulkWriteException<ObserverPartitionCounts>(_connectionId, Acknowledged(models, models), [], null, [])
    ];

    async Task Because()
    {
        foreach (var failure in _failures)
        {
            _attempts.Clear();
            _failure = failure;
            var error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));
            _errors.Add((_driverError, error));
        }
    }

    [Fact] void should_never_guess_a_prefix() => _errors.TrueForAll(_ => ReferenceEquals(_.Original, _.Surfaced)).ShouldBeTrue();
    [Fact] void should_cover_every_ambiguous_evidence_shape() => _errors.Count.ShouldEqual(12);
    [Fact] void should_not_retry_internally() => _options.Count.ShouldEqual(12);
}
