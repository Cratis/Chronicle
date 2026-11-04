// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating;

public class and_the_event_does_not_exist : given.a_compensating_event_sequence
{
    AppendResult _result;

    async Task Because() => _result = await Compensate(6UL);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_that_the_event_was_not_found() => _result.Errors.ShouldContain(AppendError.CompensatedEventNotFound);
    [Fact] void should_not_append_anything() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
