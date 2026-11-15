// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_concurrent_requests_need_the_same_graph : given.a_manager_with_shared_definitions
{
    readonly TaskCompletionSource<IProjection> _compilation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjection[] _results;

    void Establish() => _projectionFactory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_compilation.Task);

    async Task Because()
    {
        var requests = Enumerable.Range(0, 32).Select(_ => _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier)).ToArray();
        var graph = Substitute.For<IProjection>();
        graph.DefinitionFingerprint.Returns(ProjectionFactory.DefinitionFingerprintFor(_firstDefinition));
        _compilation.SetResult(graph);
        _results = (await Task.WhenAll(requests)).Select(result => result!).ToArray();
    }

    [Fact] void should_compile_only_once() => _ = _projectionFactory.Received(1).Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_share_the_validated_graph() => _results.Distinct().Count().ShouldEqual(1);
    [Fact] void should_retain_one_graph() => _manager.CachedGraphCount.ShouldEqual(1);
}
