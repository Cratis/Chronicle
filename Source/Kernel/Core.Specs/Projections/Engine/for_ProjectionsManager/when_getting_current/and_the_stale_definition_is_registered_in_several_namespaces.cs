// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_the_stale_definition_is_registered_in_several_namespaces : given.a_manager_with_shared_definitions
{
    static readonly EventStoreNamespaceName _otherNamespace = "other";
    IProjection _otherProjection;

    async Task Establish()
    {
        await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace, _otherNamespace]);
        _stored = _stored with { SubscribesToAllEvents = true };
    }

    async Task Because()
    {
        _result = await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);
        _manager.TryGet(_eventStore, _otherNamespace, _firstDefinition.Identifier, out _otherProjection);
    }

    [Fact] void should_keep_a_projection_for_the_other_namespace() => _otherProjection.ShouldNotBeNull();
    [Fact] void should_rebuild_the_other_namespace_from_the_stored_definition() => _otherProjection.DefinitionFingerprint.ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_stored));
}
