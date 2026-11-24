// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_activation_is_cancelled_by_the_runtime : given.an_observer
{
    readonly given.RecordingObserverLogger _recordedLogs = new();
    Task _activation;

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _activation = _observer.OnActivateAsync(cancellation.Token);
        await Catch.Exception(() => _activation);
    }

    [Fact] void should_end_the_activation_as_cancelled() => _activation.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_end_the_activation_as_failed() => _activation.IsFaulted.ShouldBeFalse();
    [Fact] void should_record_that_the_activation_was_cancelled() => _recordedLogs.Warnings.Any(_ => _.Contains("was cancelled by the runtime", StringComparison.Ordinal)).ShouldBeTrue();
}
