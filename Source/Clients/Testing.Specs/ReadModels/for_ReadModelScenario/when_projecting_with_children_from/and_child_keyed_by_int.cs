// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_int : Specification
{
    ReadModelScenario<BucketLedger> _scenario;
    EventSourceId _ledgerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<BucketLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new BucketAmountRecorded(1, 10m),
                new BucketAmountRecorded(2, 20m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_bucket_lines() => _scenario.Instance!.Buckets.Count().ShouldEqual(2);
    [Fact] void should_map_first_bucket_key() => _scenario.Instance!.Buckets.First().Bucket.ShouldEqual(1);
    [Fact] void should_map_first_bucket_amount() => _scenario.Instance!.Buckets.First().Amount.ShouldEqual(10m);
    [Fact] void should_map_second_bucket_key() => _scenario.Instance!.Buckets.Last().Bucket.ShouldEqual(2);
}
