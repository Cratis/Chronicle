// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap;

public class when_round_tripping_a_failed_partition_with_a_reset_retry_budget : Specification
{
    FailedPartition _restored;
    BsonDocument _document;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }

        var partition = new FailedPartition { Partition = "failed-partition" };
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 12UL });
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 15UL });
        partition.AttemptsBeforeBudgetReset = 2;
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 16UL });
        var bson = partition.ToBson();
        _document = BsonSerializer.Deserialize<BsonDocument>(bson);
        _restored = BsonSerializer.Deserialize<FailedPartition>(bson);
    }

    [Fact] void should_store_the_attempts_before_the_reset() => HasElement(nameof(FailedPartition.AttemptsBeforeBudgetReset)).ShouldBeTrue();
    [Fact] void should_not_serialize_the_derived_budget() => HasElement(nameof(FailedPartition.AttemptsInCurrentBudget)).ShouldBeFalse();
    [Fact] void should_restore_the_attempts_before_the_reset() => _restored.AttemptsBeforeBudgetReset.ShouldEqual(2);
    [Fact] void should_restore_the_attempts_in_the_current_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);

    bool HasElement(string name) => _document.Names.Any(_ => string.Equals(_, name, StringComparison.OrdinalIgnoreCase));
}
