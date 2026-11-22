// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_reading_the_same_definition_for_two_namespaces : given.the_provider
{
    GrainState<ObserverDefinition> _otherNamespaceState;

    async Task Because()
    {
        _definitionsStorage.Get(_observerId).Returns(DefinitionOn(EventSequenceId.Log));
        var otherNamespaceGrainId = GrainId.Create("type", new ObserverKey(_observerId, "an-event-store", "another-namespace", EventSequenceId.Log).ToString());
        _otherNamespaceState = new() { State = new() };
        await _provider.ReadStateAsync("name", _grainId, _grainState);
        await _provider.ReadStateAsync("name", otherNamespaceGrainId, _otherNamespaceState);
    }

    [Fact] void should_give_the_first_namespace_the_definition() => _grainState.State.ShouldEqual(DefinitionOn(EventSequenceId.Log));
    [Fact] void should_give_the_second_namespace_the_same_definition() => _otherNamespaceState.State.ShouldEqual(_grainState.State);
}
