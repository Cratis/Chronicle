// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// Building the engine projection for every definition x namespace pair used to happen before the subscribe gate, so a
/// large registration built thousands at once - CPU the subscribes then competed for. Building now happens inside the
/// gate, so it is bounded by the same limit as the subscribes themselves.
/// </summary>
public class and_the_projections_are_built_for_many_pairs_at_once : given.a_projections_manager_grain
{
    const int DefinitionCount = 40;
    static readonly Lock _lock = new();

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
                lock (_lock)
                {
                    _current++;
                    _maxObservedConcurrency = Math.Max(_maxObservedConcurrency, _current);
                }

                await Task.Delay(20);

                lock (_lock)
                {
                    _current--;
                }

                return _engineProjection;
            });
    }

    async Task Because() => await _grain.Register(_incoming);

    [Fact] void should_have_built_a_projection_for_every_definition() => _projectionFactory.ReceivedWithAnyArgs(DefinitionCount).Create(default, default!, default!, default!, default!);
    [Fact] void should_never_build_more_at_once_than_the_subscribe_bound() => (_maxObservedConcurrency <= 8).ShouldBeTrue();
    [Fact] void should_have_actually_built_concurrently() => _maxObservedConcurrency.ShouldBeGreaterThan(1);
}
