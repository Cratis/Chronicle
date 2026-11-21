// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_reading_the_same_definition_while_a_read_is_running : given.the_provider
{
    TaskCompletionSource<ObserverDefinition> _stored;
    GrainState<ObserverDefinition> _otherNamespaceState;
    GrainId _otherNamespaceGrainId;

    void Establish()
    {
        _stored = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _definitionsStorage.Get(_observerId).Returns(_stored.Task);
        _otherNamespaceGrainId = GrainId.Create("type", new ObserverKey(_observerId, "an-event-store", "another-namespace", EventSequenceId.Log).ToString());
        _otherNamespaceState = new() { State = new() };
    }

    async Task Because()
    {
        var first = _provider.ReadStateAsync("name", _grainId, _grainState);
        var second = _provider.ReadStateAsync("name", _otherNamespaceGrainId, _otherNamespaceState);
        _stored.SetResult(DefinitionOn(_otherEventSequenceId));
        await first;
        await second;
    }

    [Fact] void should_read_storage_once() => _definitionsStorage.Received(1).Get(_observerId);
    [Fact] void should_give_the_first_activation_the_definition() => _grainState.State.EventSequenceId.ShouldEqual(_otherEventSequenceId);
    [Fact] void should_give_the_second_activation_the_definition() => _otherNamespaceState.State.EventSequenceId.ShouldEqual(_otherEventSequenceId);
}
