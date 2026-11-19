// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_pin_has_aged : given.a_window_mismatch
{
    readonly given.AdjustableTimeProvider _clock = new();

    protected override TimeProvider CreateTimeProvider() => _clock;

    async Task Establish()
    {
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult(Windows((SiloAddress.Zero, [Guid.NewGuid()]))),
            Task.FromResult(Windows()));
        await _jobStep.FlushCheckpoint(start: true);
        _clock.Advance(TimeSpan.FromSeconds(90));
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
    }

    async Task Because() => _error = await Catch.Exception(() => _jobStep.ReportNewSuccessfullyHandledEvent(3UL));

    [Fact] void should_report_the_age_of_the_pin() => Message.ShouldContain("pin age 00:01:30");
}
