// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_saving_an_unchanged_definition : given.a_registry
{
    ObserverDefinition _before;
    ObserverDefinition _after;
    long _revisionBefore;
    long _revisionAfter;

    async Task Because()
    {
        _before = await _registry.Get(_observerId);
        _revisionBefore = (await _registry.GetSnapshot()).Revision;
        await _registry.Save(DefinitionOf(_observerId, _firstEventType));
        _after = await _registry.Get(_observerId);
        _revisionAfter = (await _registry.GetSnapshot()).Revision;
    }

    [Fact] void should_keep_the_instance() => ReferenceEquals(_before, _after).ShouldBeTrue();
    [Fact] void should_keep_the_revision() => _revisionAfter.ShouldEqual(_revisionBefore);
}
