// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_subject_was_erased_and_a_guessed_value_is_retried : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish()
    {
        await Protect(erased: true);
        _request.Content["name"] = "Guess";
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_not_be_a_value_oracle() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppendedAfterErasure);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
