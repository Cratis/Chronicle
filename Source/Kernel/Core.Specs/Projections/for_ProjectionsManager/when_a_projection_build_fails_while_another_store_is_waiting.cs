// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// A failed build still reports its failure and returns admission to queued healthy work.
/// </summary>
public class when_a_projection_build_fails_while_another_store_is_waiting : given.two_projections_managers_sharing_admission
{
    readonly TaskCompletionSource _failedBuildEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _error;
    bool _capacityIntact;

    void Establish()
    {
        HoldBuilds();
        _secondFactory.Create(default!, default!, default!, default!, default!).ReturnsForAnyArgs(async _ =>
        {
            _failedBuildEntered.TrySetResult();
            await _release.Task;
            return await Task.FromException<Engine.IProjection>(new InvalidOperationException("Build failed"));
        });
    }

    async Task Because()
    {
        var failing = _secondGrain.Register(_secondDefinitions.Take(8));
        await _failedBuildEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var healthy = _grain.Register(_firstDefinitions);
        _release.TrySetResult();
        _error = await Catch.Exception(() => failing);
        await healthy;
        _capacityIntact = true;
        for (var index = 0; index < 8; index++) _capacityIntact &= await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _capacityIntact &= !await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < 8; index++) _throttle.Release();
    }

    [Fact] void should_report_the_build_failure() => _error.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_complete_the_healthy_store() => _state.Projections.Count().ShouldEqual(12);
    [Fact] void should_return_exactly_the_granted_capacity() => _capacityIntact.ShouldBeTrue();
}
