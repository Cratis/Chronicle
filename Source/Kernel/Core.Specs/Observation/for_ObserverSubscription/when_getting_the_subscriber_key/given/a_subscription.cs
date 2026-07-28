// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscription.when_getting_the_subscriber_key.given;

public class a_subscription : Specification
{
    protected static readonly ObserverKey observer_key = new(
        "3ec5dbcb-3f7c-4a2b-9d6f-4a1f2b0c8e11",
        "some-event-store",
        "some-namespace",
        EventSequenceId.Log);

    protected static readonly SiloAddress silo_address = SiloAddress.FromParsableString("127.0.0.1:11111@1");

    protected Key[] partitions;

    void Establish() => partitions = [.. Enumerable.Range(0, 8).Select(index => (Key)$"partition-{index}")];

    protected static ObserverSubscription SubscriptionFor(Type subscriberType) => new(
        observer_key.ObserverId,
        observer_key,
        [],
        subscriberType,
        silo_address);
}
