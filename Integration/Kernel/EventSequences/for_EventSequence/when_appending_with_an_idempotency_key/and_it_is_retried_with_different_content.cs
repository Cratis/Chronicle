// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key.and_it_is_retried_with_different_content.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

[Collection(ChronicleCollection.Name)]
public class and_it_is_retried_with_different_content(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_log_with_idempotency_keys(fixture)
    {
        async Task Because()
        {
            First = await Append();
            _request.Content[nameof(given.CustomerNamed.Name)] = "Bob";
            Retry = await Append();
            await CountEvents();
        }
    }

    [Fact] void should_append_the_first_attempt() => Context.First.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_the_retry() => Context.Retry.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_content_conflict() => Context.Retry.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Content);
    [Fact] void should_store_one_event() => Context.Count.ShouldEqual(1UL);
}
