// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_writing_a_definition_the_registry_holds_differently : given.the_provider
{
    ObserverDefinition _own;

    void Establish()
    {
        _own = DefinitionOn(_observerKey.EventSequenceId);
        _grainState.State = _own;
        _definitionsStorage.Get(_observerId).Returns(_own with { IsReplayable = false });
    }

    Task Because() => _provider.WriteStateAsync("name", _grainId, _grainState);

    [Fact] void should_keep_its_own_definition() => ReferenceEquals(_grainState.State, _own).ShouldBeTrue();
}
