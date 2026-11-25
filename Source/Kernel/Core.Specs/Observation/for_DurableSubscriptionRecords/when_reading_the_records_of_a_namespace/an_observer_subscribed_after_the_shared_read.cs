// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_reading_the_records_of_a_namespace;

public class an_observer_subscribed_after_the_shared_read : given.durable_subscription_records
{
    static readonly ObserverKey _late = new("the-late-observer", "the-event-store", "the-namespace", EventSequenceId.Log);

    ObserverDurableSubscription? _found;

    async Task Establish()
    {
        Create();
        await _records.Get(_late);
        _stored[_late.ObserverId] = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("the-connection", "silo")]);
        _time.Advance(DurableSubscriptionRecords.RecheckMissAfter + TimeSpan.FromSeconds(1));
    }

    async Task Because() => _found = await _records.Get(_late);

    [Fact] void should_find_the_record_in_storage() => _found.ShouldNotBeNull();
}
