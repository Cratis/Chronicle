// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// Building the engine projection for every definition x namespace pair used to happen before the subscribe gate, so a
/// large registration built thousands at once - CPU the subscribes then competed for. Building now happens inside the
/// gate, so it is bounded by the same limit as the subscribes themselves. The first builds are held until as many are
/// in flight as the gate allows; an unbounded fan-out would by then have started every one of them.
/// </summary>
public class and_the_projections_are_built_for_many_pairs_at_once : given.a_projections_manager_grain
{
    const int DefinitionCount = 40;
    const int SubscribeBound = 8;
    static readonly Lock _lock = new();

    readonly TaskCompletionSource _boundReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _current;
    int _maxObservedConcurrency;
    ProjectionDefinition[] _incoming = [];

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _incoming = [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];

        _projectionFactory
            .Create(default, default!, default!, default!, default!)
            .ReturnsForAnyArgs(async _ =>
            {
                int inFlight;
                lock (_lock)
                {
                    inFlight = ++_current;
                    _maxObservedConcurrency = Math.Max(_maxObservedConcurrency, inFlight);
                }

                if (inFlight >= SubscribeBound)
                {
                    _boundReached.TrySetResult();
                }

                await _release.Task;

                lock (_lock)
                {
                    _current--;
                }

                return _engineProjection;
            });
    }

    async Task Because()
    {
        var registering = _grain.Register(_incoming);
        await _boundReached.Task.WaitAsync(TimeSpan.FromSeconds(30));
        _release.SetResult();
        await registering;
    }

    [Fact] void should_have_built_a_projection_for_every_definition() => _projectionFactory.ReceivedWithAnyArgs(DefinitionCount).Create(default, default!, default!, default!, default!);
    [Fact] void should_never_build_more_at_once_than_the_subscribe_bound() => _maxObservedConcurrency.ShouldEqual(SubscribeBound);
}
