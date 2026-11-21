// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_reading_the_records_of_a_namespace;

public class an_observer_missing_from_a_recent_read : given.durable_subscription_records
{
    static readonly ObserverKey _unknown = new("an-unknown-observer", "the-event-store", "the-namespace", EventSequenceId.Log);

    async Task Establish()
    {
        Create();
        await _records.Get(_unknown);
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await _records.Get(_unknown);
        }
    }

    [Fact] async Task should_not_read_storage_again_for_every_ask() => await _states.Received(1).GetDurableSubscriptions(EventSequenceId.Log);
}
