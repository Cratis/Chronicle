// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_reading_the_records_of_a_namespace;

public class more_than_once_within_the_cache_period : given.durable_subscription_records
{
    async Task Establish()
    {
        Create();
        await _records.Get(_observer);
        _time.Advance(TimeSpan.FromSeconds(10));
    }

    async Task Because() => await _records.Get(_observer);

    [Fact] async Task should_read_storage_once() => await _states.Received(1).GetDurableSubscriptions(EventSequenceId.Log);
}
