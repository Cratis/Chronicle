// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_with_different_content : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        Durable();
        _request.Content["different"] = true;
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_content_conflict() => _result.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Content);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
