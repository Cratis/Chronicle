// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters;

public class when_round_tripping_pending_catch_up_repairs : Specification
{
    KernelObserverState _result;
    KernelObserverState _legacy;
    KernelObserverState _withFailures;

    void Because()
    {
        var source = KernelObserverState.Empty with { Identifier = "observer", NextEventSequenceNumber = 13UL, PendingCatchUpRepairs = [new("B", 10UL, 13UL)] };
        var document = source.ToMongoDB().ToBsonDocument();
        _result = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _withFailures = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
        document.Remove(nameof(ObserverState.PendingCatchUpRepairs));
        _legacy = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
    }

    [Fact] void should_preserve_the_range() => _result.PendingCatchUpRepairs!.ShouldContainOnly(new Storage.Observation.PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_map_repairs_with_failed_partitions() => _withFailures.PendingCatchUpRepairs!.ShouldContainOnly(new Storage.Observation.PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_treat_an_absent_legacy_field_as_no_repairs() => _legacy.PendingCatchUpRepairs.ShouldBeNull();
}
