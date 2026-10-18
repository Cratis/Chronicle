// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_the_registered_definition_is_stale : given.a_manager_with_shared_definitions
{
    IProjection _previous;

    async Task Establish()
    {
        await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _previous);
        _stored = _stored with { SubscribesToAllEvents = true };
    }

    async Task Because() => _result = await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);

    [Fact] void should_rebuild_the_stale_projection() => _result.ShouldNotBeSame(_previous);
    [Fact] void should_use_the_stored_definition() => _result.DefinitionFingerprint.ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_stored));
    [Fact] void should_replace_the_registered_entry() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeTrue();
}
