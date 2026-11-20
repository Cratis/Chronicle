// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_reading_the_records_of_a_namespace;

public class fresh : given.durable_subscription_records
{
    async Task Establish()
    {
        Create();
        await _records.Get(_observer);
    }

    async Task Because() => await _records.GetAll(_observer.EventStore, _observer.Namespace, EventSequenceId.Log, fresh: true);

    [Fact] async Task should_read_storage_again() => await _states.Received(2).GetDurableSubscriptions(EventSequenceId.Log);
}
