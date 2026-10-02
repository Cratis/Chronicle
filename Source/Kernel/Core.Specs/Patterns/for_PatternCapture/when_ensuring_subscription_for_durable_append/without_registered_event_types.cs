// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class without_registered_event_types : given.a_pattern_capture
{
    bool _subscribed;

    void Establish() => EventTypesAre();

    async Task Because() => _subscribed = await _capture.EnsureSubscribedForDurableAppend(_eventStore, _namespace);

    [Fact] void should_report_that_subscription_was_skipped() => _subscribed.ShouldBeFalse();
    [Fact] async Task should_not_write_a_definition() => await _reactors.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
}
