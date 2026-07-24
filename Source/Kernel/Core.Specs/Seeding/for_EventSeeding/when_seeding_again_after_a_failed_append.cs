// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_again_after_a_failed_append : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;
    Exception _firstError;
    int _appendCount;

    void Establish()
    {
        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null),
            new SeedingEntry("event-source-2", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test2\"}", null)
        ];

        // Fail the first append to simulate a transient failure, then succeed on the retry.
        _eventSequence
            .When(x => x.AppendMany(
                Arg.Any<IEnumerable<EventToAppend>>(),
                Arg.Any<CorrelationId>(),
                Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
                Arg.Any<Concepts.Identities.Identity>(),
                Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>()))
            .Do(_ =>
            {
                _appendCount++;
                if (_appendCount == 1)
                {
                    throw new Exception("Simulated transient append failure");
                }
            });
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(() => _grain.Seed(_entries));
        await _grain.Seed(_entries);
    }

    [Fact] void should_fail_the_first_attempt() => _firstError.ShouldNotBeNull();

    [Fact]
    void should_retry_the_append_on_the_second_attempt() => _eventSequence.Received(2).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 2),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_write_state_after_the_successful_retry() => _state.Received(1).WriteStateAsync();
}
