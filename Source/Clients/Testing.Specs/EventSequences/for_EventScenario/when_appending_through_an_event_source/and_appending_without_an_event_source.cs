// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class and_appending_without_an_event_source : given_a_scenario_with_an_event_source
{
    AppendedEvent _stored;

    async Task Because()
    {
        await _scenario.EventLog.Append(_source, new TestEvent("hello"));
        _stored = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Single();
    }

    [Fact] void should_normalize_to_a_not_set_event_source() => _stored.Context.EventSource.ShouldEqual(EventSourceName.NotSet);
    [Fact] void should_keep_the_default_event_source_type() => _stored.Context.EventSourceType.ShouldEqual(EventSourceType.Default);
}
