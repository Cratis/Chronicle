// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_an_earlier_cleanup_left_a_fence : given.all_dependencies
{
    Exception _error;

    void Establish()
    {
        _observerInFirstNamespace.GetState().Returns(ObserverState.Empty with { AlertDisposition = AlertDisposition.Removing });
        _observerInSecondNamespace.Remove().Returns(Task.FromException(new ObserverRemovalNotAllowed(new(_observerId, _eventStore, _secondNamespace, EventSequenceId.Log))));
    }

    async Task Because() => _error = await Catch.Exception(Remove);

    [Fact] void should_propagate_the_failure() => _error.ShouldBeOfExactType<ObserverRemovalNotAllowed>();
    [Fact] async Task should_keep_the_earlier_cleanup_fenced() => await _observerInFirstNamespace.DidNotReceive().CancelRemoval();
    [Fact] async Task should_cancel_only_the_new_attempt() => await _observerInSecondNamespace.Received(1).CancelRemoval();
}
