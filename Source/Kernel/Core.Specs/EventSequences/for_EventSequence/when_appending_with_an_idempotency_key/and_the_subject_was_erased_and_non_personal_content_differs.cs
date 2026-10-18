// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_subject_was_erased_and_non_personal_content_differs : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish()
    {
        await Protect(erased: true);
        _request.Content["count"] = 2;
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse_the_changed_plaintext() => _result.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Content);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
