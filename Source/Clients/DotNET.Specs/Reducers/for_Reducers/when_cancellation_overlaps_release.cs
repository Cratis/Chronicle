// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_cancellation_overlaps_release : given.two_discovered_reducers
{
    readonly TaskCompletionSource _releaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _continueRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _cancellationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenRegistration _firstRegistration;
    CancellationTokenRegistration _secondRegistration;
    bool _releaseOwnsMonitor;
    bool _cancellationWasExcluded;

    void Establish()
    {
        _firstRegistration = _firstToken.Register(() =>
        {
            _releaseOwnsMonitor = Monitor.IsEntered(_reducers.ReadModelOwnershipSync);
            _releaseEntered.TrySetResult();
            _continueRelease.Task.GetAwaiter().GetResult();
        });
        _secondRegistration = _secondToken.Register(() => _cancellationEntered.TrySetResult());
    }

    async Task Because()
    {
        var release = Task.Run(_reducers.Release);
        var cancellation = Task.CompletedTask;
        try
        {
            await _releaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation = Task.Run(() =>
            {
                var acquired = Monitor.TryEnter(_reducers.ReadModelOwnershipSync);
                _cancellationWasExcluded = !acquired;
                if (acquired) Monitor.Exit(_reducers.ReadModelOwnershipSync);
                _second.Disconnect();
            });
            await _cancellationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            _continueRelease.TrySetResult();
        }
        await Task.WhenAll(release, cancellation).WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy()
    {
        _continueRelease.TrySetResult();
        _firstRegistration.Dispose();
        _secondRegistration.Dispose();
    }

    [Fact] void should_hold_the_existing_ownership_monitor_during_release() => _releaseOwnsMonitor.ShouldBeTrue();
    [Fact] void should_exclude_the_competing_cancellation_from_the_monitor() => _cancellationWasExcluded.ShouldBeTrue();
    [Fact] void should_empty_the_model_index() => _reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_empty_the_first_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(_first.ReducerType)).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_empty_the_second_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(_second.ReducerType)).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_dispose_the_first_handler() => Catch.Exception(() => _ = _first.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_dispose_the_second_handler() => Catch.Exception(() => _ = _second.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
}
