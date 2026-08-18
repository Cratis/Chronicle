// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

/// <summary>
/// The in-process event store has no kernel behind it, so an append never reaches an observer. Waiting for observer
/// completion must fail loudly rather than report success - the same honesty the scenario surface owes a spec author.
/// </summary>
public class when_waiting_for_observer_completion : Specification
{
    IClientArtifactsProvider _clientArtifactsProvider;
    EventStoreForTesting _eventStore;
    AppendResult _appendResult;
    Exception _exception;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _clientArtifactsProvider.EventTypes.Returns([typeof(ModuleCreated)]);
        _eventStore = new EventStoreForTesting(null, _clientArtifactsProvider);
    }

    async Task Because()
    {
        _appendResult = await _eventStore.EventLog.Append(EventSourceId.New(), new ModuleCreated("Some module"));
        _exception = await Catch.Exception(async () => await _appendResult.WaitForCompletion());
    }

    [Fact] void should_have_appended_the_event() => _appendResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_that_observers_completed() => _exception.ShouldBeOfExactType<CannotWaitForObserverCompletion>();
}
