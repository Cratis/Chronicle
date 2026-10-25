// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Generations.Bindings;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_the_generation_floor_is_not_established : given.a_generation_projection_resolver
{
    IReadOnlyCollection<GenerationExecution> _result;

    void Establish() => _keys
        .For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>())
        .ThrowsAsync(new GenerationFloorNotEstablished(Store));

    async Task Because() => _result = await _resolver.ResolveCataloged(Store, GenerationExecutions.Namespace, GenerationExecutions.Projection);

    [Fact] void should_resolve_nothing() => _result.ShouldBeEmpty();
    [Fact] void should_not_read_the_catalog() => _bindings.DidNotReceive().Get(Arg.Any<EventStoreName>(), Arg.Any<MaterializationKey>());
}
