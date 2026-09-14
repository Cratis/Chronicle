// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A kernel restart activates one <see cref="ProjectionsManager"/> per event store, and each one fans out
/// subscribing every projection to every namespace in full parallel. Subscribing an observer also drives it to
/// recover every failed partition it remembers, so with enough projections and namespaces across a cluster the
/// unbounded fan-out can saturate the silo's CPU during a restart. This proves the bound actually holds: with far
/// more definitions than the concurrency limit registered at once, the number of subscribe calls in flight at any
/// point never exceeds it.
/// </summary>
public class and_more_definitions_than_the_concurrency_limit_are_new : given.a_projections_manager_grain
{
    const int DefinitionCount = 40;
    static readonly Lock _lock = new();

    int _current;
    int _maxObservedConcurrency;
    ProjectionDefinition[] _incoming = [];

    void Establish()
    {
        var definitions = new ProjectionDefinition[DefinitionCount];
        for (var i = 0; i < DefinitionCount; i++)
        {
            definitions[i] = CreateDefinition($"the-projection-{i}", "the-read-model");
        }

        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _observerGrain
            .Subscribe<IProjectionObserverSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>())
            .Returns(async _ =>
            {
                lock (_lock)
                {
                    _current++;
                    if (_current > _maxObservedConcurrency)
                    {
                        _maxObservedConcurrency = _current;
                    }
                }

                // Long enough that, without a bound, every one of the DefinitionCount subscribes would overlap.
                await Task.Delay(20);

                lock (_lock)
                {
                    _current--;
                }
            });

        _incoming = definitions;
    }

    async Task Because() => await _grain.Register(_incoming);

    [Fact] void should_have_subscribed_every_definition() => _observerGrain
        .Received(DefinitionCount)
        .Subscribe<IProjectionObserverSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>());

    [Fact] void should_never_exceed_the_concurrency_bound() => (_maxObservedConcurrency <= 8).ShouldBeTrue();

    [Fact] void should_have_actually_run_concurrently() => _maxObservedConcurrency.ShouldBeGreaterThan(1);
}
