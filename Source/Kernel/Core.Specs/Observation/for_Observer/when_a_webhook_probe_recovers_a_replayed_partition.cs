// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_recovers_a_replayed_partition : given.a_webhook_ready_to_probe
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009"));
    ObserverSubscriberContext _context;

    void Establish()
    {
        _failedPartitionsState.Partitions.Single().ReplayEpoch = _epoch;
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Do<ObserverSubscriberContext>(_ => _context = _), Arg.Any<CancellationToken>()).Returns(ObserverSubscriberResult.Ok(42UL));
    }

    Task Because() => Tick();

    [Fact] void should_deliver_the_probe_with_the_replay_epoch() => _context.ReplayEpoch.ShouldEqual(_epoch);
}
