// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_definition_changes_during_a_probe : given.a_webhook_ready_to_probe
{
    void Establish() => _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(_ =>
    {
        _webhookDefinitions.Get(_observerId).Returns(_webhookDefinition with { Target = _webhookDefinition.Target with { Url = "https://other.example/hook" } });
        return ObserverSubscriberResult.Ok(42UL);
    });

    Task Because() => Tick();

    [Fact] void should_not_apply_the_stale_success() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_move_the_cursor_for_the_new_definition() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
}
