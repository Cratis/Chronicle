// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_none_is_registered : given.a_manager_with_shared_definitions
{
    async Task Because() => _result = await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);

    [Fact] void should_build_from_shared_storage() => _result.DefinitionFingerprint.ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_stored));
    [Fact] void should_register_the_projection() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeTrue();
}
