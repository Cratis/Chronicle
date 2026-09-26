// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_the_batch_has_no_events : given.an_event_sequence_with_metadata
{
    AppendManyResult _result;

    async Task Because() => _result = await _eventSequence.AppendMany(
        [],
        concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [_source] = new(0, _source) });

    [Fact] void should_validate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_notify_subscribers() => _notifications.ShouldBeNull();
}
