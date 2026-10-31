// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_storage_throws_and_the_slot_holds_another_event : given.an_append_with_settlement
{
    void Establish()
    {
        _throwAfterWrite = true;
        _foreign = true;
    }
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_report_unknown() => _result.Errors.ShouldContain((AppendError)"AppendOutcomeUnknown");
}
