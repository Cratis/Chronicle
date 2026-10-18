// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_the_definition_is_unchanged : given.a_manager_with_shared_definitions
{
    IProjection _previous;

    async Task Establish()
    {
        await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _previous);
    }

    async Task Because() => _result = await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);

    [Fact] void should_reuse_the_current_projection() => _result.ShouldBeSame(_previous);
}
