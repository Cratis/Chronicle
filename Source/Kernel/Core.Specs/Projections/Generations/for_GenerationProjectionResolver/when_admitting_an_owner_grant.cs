// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_admitting_an_owner_grant : given.a_generation_projection_resolver
{
    GenerationExecution _generation;
    GenerationWriterGrant _grant;
    GenerationSinkContext _context;

    void Establish()
    {
        _generation = GenerationExecutions.Create(_projection);
        _grant = new(Store, _generation.Binding, new(Guid.Parse("5f0e4a8d-2b7c-4c19-8e3a-6d1f9b2a7c40")), 7);
        _grants.GetAdmitted(Store, _generation.Binding).Returns(_grant);
    }

    async Task Because() => _context = await _resolver.Admit(Store, _generation);

    [Fact] void should_carry_the_stored_grant() => _context.Grant.ShouldEqual(_grant);
    [Fact] void should_carry_the_binding() => _context.Binding.ShouldEqual(_generation.Binding);
    [Fact] void should_carry_the_execution() => _context.Execution.ShouldEqual(_generation.Execution);
    [Fact] void should_never_acquire_a_grant() => _grants.DidNotReceive().TryAcquire(Arg.Any<EventStoreName>(), Arg.Any<GenerationObserverBinding>(), Arg.Any<GenerationBuildOperationId>(), Arg.Any<long>());
}
