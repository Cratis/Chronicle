// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_writing_a_definition_the_registry_holds_afterwards : given.the_provider
{
    ObserverDefinition _own;
    ObserverDefinition _registryInstance;

    void Establish()
    {
        _own = DefinitionOn(_observerKey.EventSequenceId);
        _registryInstance = DefinitionOn(_observerKey.EventSequenceId) with { EventTypes = EventTypeSet.Empty };
        _grainState.State = _own;
        _definitionsStorage.Get(_observerId).Returns(_registryInstance);
    }

    Task Because() => _provider.WriteStateAsync("name", _grainId, _grainState);

    [Fact] void should_hold_the_registry_instance() => ReferenceEquals(_grainState.State, _registryInstance).ShouldBeTrue();
}
