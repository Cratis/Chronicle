// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_registration_races_with_compilation_and_namespace_discovery : given.a_manager_with_shared_definitions
{
    readonly TaskCompletionSource<IProjection> _initialCompilation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjection _initial;

    void Establish()
    {
        _initial = Substitute.For<IProjection>();
        _initial.DefinitionFingerprint.Returns(ProjectionFactory.DefinitionFingerprintFor(_firstDefinition));
        _projectionFactory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Is<ProjectionDefinition>(definition => definition == _firstDefinition), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_initialCompilation.Task);
    }

    async Task Because()
    {
        var compiling = _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);
        _stored = _firstDefinition with { IsRewindable = !_firstDefinition.IsRewindable };
        var registering = _manager.Register(_eventStore, [_stored], [_firstReadModelDefinition], [_namespace]);
        var discovering = _manager.AddNamespace(_eventStore, "new-tenant", [_firstReadModelDefinition]);
        _initialCompilation.SetResult(_initial);
        await Task.WhenAll(compiling, registering, discovering);
        _result = (await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier))!;
    }

    [Fact] void should_publish_the_updated_definition() => _result.DefinitionFingerprint.ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_stored));
    [Fact] void should_not_reuse_the_obsolete_graph() => _result.ShouldNotEqual(_initial);
    [Fact] void should_not_compile_for_namespace_discovery() => _manager.TryGet(_eventStore, "new-tenant", _firstDefinition.Identifier, out _).ShouldBeFalse();
    [Fact] void should_retain_only_the_current_graph() => _manager.CachedGraphCount.ShouldEqual(1);
}
