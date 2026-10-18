// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_no_definition_exists : given.a_manager_with_shared_definitions
{
    async Task Establish()
    {
        await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        _storage.GetEventStore(_eventStore).Projections.Has(_firstDefinition.Identifier).Returns(false);
    }

    async Task Because() => _result = await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);

    [Fact] void should_not_return_a_stale_projection() => _result.ShouldBeNull();
    [Fact] void should_remove_the_registered_entry() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
}
