// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class without_registered_event_types : given.a_pattern_capture
{
    void Establish() => EventTypesAre();

    async Task Because() => await _capture.RecoverSubscription(_eventStore, _namespace);

    [Fact] async Task should_not_check_subscription_readiness() => await _observer.DidNotReceive().NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>());
    [Fact] async Task should_not_write_a_definition() => await _reactors.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
}
