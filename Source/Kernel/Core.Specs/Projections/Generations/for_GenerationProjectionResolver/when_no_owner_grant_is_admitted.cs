// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_no_owner_grant_is_admitted : given.a_generation_projection_resolver
{
    GenerationExecution _generation;
    Exception _error;

    void Establish()
    {
        _generation = GenerationExecutions.Create(_projection);
        _grants.GetAdmitted(Store, _generation.Binding).Returns((GenerationWriterGrant?)null);
    }

    async Task Because() => _error = await Catch.Exception(async () => await _resolver.Admit(Store, _generation));

    [Fact] void should_refuse_to_admit() => _error.ShouldBeOfExactType<GenerationWriterGrantNotAdmitted>();
    [Fact] void should_never_acquire_a_grant() => _grants.DidNotReceive().TryAcquire(Arg.Any<EventStoreName>(), Arg.Any<GenerationObserverBinding>(), Arg.Any<GenerationBuildOperationId>(), Arg.Any<long>());
}
