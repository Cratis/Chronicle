// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The kernel's AppendOutcome and IdempotencyConflictReason share their full names with the .NET client's types,
// and this project references both assemblies, so the outcome is compared by name.

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key.and_it_is_retried_with_the_same_content.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

[Collection(ChronicleCollection.Name)]
public class and_it_is_retried_with_the_same_content(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_log_with_idempotency_keys(fixture)
    {
        async Task Because()
        {
            First = await Append();
            Retry = await Append();
            await CountEvents();
        }
    }

    [Fact] void should_append_the_first_attempt() => Context.First.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_append_errors() => Context.First.Result.Errors.ShouldBeEmpty();
    [Fact] void should_not_report_constraint_violations() => Context.First.Result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_not_report_concurrency_violations() => Context.First.Result.ConcurrencyViolation.ShouldBeNull();
    [Fact] void should_settle_the_retry() => Context.Retry.Outcome.ToString().ShouldEqual("AlreadyAppended");
    [Fact] void should_return_the_original_sequence_number() => Context.Retry.Result.SequenceNumber.ShouldEqual(Context.First.Result.SequenceNumber);
    [Fact] void should_store_one_event() => Context.Count.ShouldEqual(1UL);
}
