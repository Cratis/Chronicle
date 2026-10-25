// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_reactivating_on_another_silo : given.a_generation_bound_subscriber
{
    readonly List<GenerationSinkContext> _contexts = [];
    IWriterGrants _grants;
    GenerationWriterGrant _grant;

    void Establish()
    {
        _grants = Substitute.For<IWriterGrants>();
        _grant = new(_key.EventStore, _execution.Binding, new(Guid.Parse("0b6d2f4e-7a1c-4e8b-9d3f-2c5a8e1b7f60")), 4);
        _grants.GetAdmitted(_key.EventStore, _execution.Binding).Returns(_grant);
        var owner = new GenerationProjectionResolver(
            Substitute.For<IGenerationEnablement>(),
            Substitute.For<IGenerationBuildStorage>(),
            new ForwardingWriterGrants(_grants),
            Substitute.For<IMaterializationKeys>(),
            Substitute.For<IMaterializationBindingStorage>(),
            Substitute.For<IProjectionFactory>(),
            new GenerationExecutionSpecifications(Globals.JsonSerializerOptions));
        _generations.Admit(_key.EventStore, _execution).Returns(_ => owner.Admit(_key.EventStore, _execution));
        _generationPipelines
            .GetFor(_key.EventStore, _key.Namespace, _generationProjection, Arg.Do<GenerationSinkContext>(_contexts.Add))
            .Returns(_generationPipeline);
    }

    async Task Because()
    {
        await Activate();
        await Activate();
    }

    [Fact] void should_build_both_pipelines() => _contexts.Count.ShouldEqual(2);
    [Fact] void should_build_equal_contexts() => _contexts[1].ShouldEqual(_contexts[0]);
    [Fact] void should_use_the_owner_grant() => _contexts[0].Grant.ShouldEqual(_grant);
    [Fact] void should_never_acquire_a_grant() => _grants.DidNotReceive().TryAcquire(Arg.Any<EventStoreName>(), Arg.Any<GenerationObserverBinding>(), Arg.Any<GenerationBuildOperationId>(), Arg.Any<long>());
}
