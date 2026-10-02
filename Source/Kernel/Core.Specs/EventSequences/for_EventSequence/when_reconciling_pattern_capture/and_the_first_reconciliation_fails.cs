// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_the_first_reconciliation_fails : given.an_event_sequence_with_pattern_capture
{
    Exception _initialError;
    int _attempts;

    async Task Establish()
    {
        _patternCapture.RecoverSubscription(EventStore, EventStoreNamespace).Returns(_ =>
        {
            if (++_attempts == 1)
            {
                return Task.FromException(new InvalidOperationException());
            }

            _captureIsSubscribed = true;
            return Task.CompletedTask;
        });
        _captureIsSubscribed = false;
        _initialError = await Catch.Exception(() => _silo.TimerRegistry.FireAllAsync());
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_not_propagate_the_analytics_failure() => _initialError.ShouldBeNull();
    [Fact] void should_recover_on_the_next_tick() => _captureIsSubscribed.ShouldBeTrue();
}
