// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_the_stored_hash_is_not_set : given.an_append_with_settlement
{
    void Establish()
    {
        _throwAfterWrite = true;
        _unsetHash = true;
    }
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_report_unknown() => _result.Errors.ShouldContain((AppendError)"AppendOutcomeUnknown");
}
