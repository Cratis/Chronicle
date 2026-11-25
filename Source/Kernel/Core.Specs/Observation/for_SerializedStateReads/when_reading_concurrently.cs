// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SerializedStateReads;

public class when_reading_concurrently : Specification
{
    SerializedStateReads _reads;
    TaskCompletionSource _firstStarted;
    TaskCompletionSource _releaseFirst;
    TaskCompletionSource _secondStarted;
    bool _secondStartedWhileFirstWasInFlight;

    void Establish()
    {
        _reads = new();
        _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var first = _reads.Read(async () =>
        {
            _firstStarted.SetResult();
            await _releaseFirst.Task;
        });
        await _firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        var second = _reads.Read(() =>
        {
            _secondStarted.SetResult();
            return Task.CompletedTask;
        });
        _secondStartedWhileFirstWasInFlight = _secondStarted.Task.IsCompleted;

        _releaseFirst.SetResult();
        await first;
        await second;
    }

    [Fact] void should_not_start_the_second_read_while_the_first_is_in_flight() => _secondStartedWhileFirstWasInFlight.ShouldBeFalse();
    [Fact] void should_perform_the_second_read_once_the_first_is_done() => _secondStarted.Task.IsCompletedSuccessfully.ShouldBeTrue();
}
