// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_single_event_that_was_already_appended : given.an_idempotent_append
{
    AppendResult _result;

    void Establish() => _singleResponse.Outcome = Contracts.EventSequences.AppendOutcome.AlreadyAppended;

    async Task Because() => _result = await _eventSequence.AppendOnce("source", "event", _key);

    [Fact] void should_report_it_as_already_appended() => _result.WasAlreadyAppended.ShouldBeTrue();
    [Fact] void should_still_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_notify() => _notifications.ShouldEqual(0);
}
