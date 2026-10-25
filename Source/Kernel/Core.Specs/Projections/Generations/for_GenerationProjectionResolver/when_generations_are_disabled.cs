// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_generations_are_disabled : given.a_generation_projection_resolver
{
    GenerationExecution? _result;
    IReadOnlyCollection<GenerationExecution> _cataloged;

    void Establish()
    {
        _enablement.Enabled.Returns(false);
        Bind(_binding);
    }

    async Task Because()
    {
        _result = await Resolve(_binding.Identity.ObserverId);
        _cataloged = await _resolver.ResolveCataloged(Store, GenerationExecutions.Namespace, GenerationExecutions.Projection);
    }

    [Fact] void should_not_resolve_an_execution() => _result.ShouldBeNull();
    [Fact] void should_not_resolve_cataloged_generations() => _cataloged.ShouldBeEmpty();
    [Fact] void should_not_read_the_observer_binding() => _builds.DidNotReceive().ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>());
}
