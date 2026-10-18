// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_missing_state_in_an_initialized_store : given.the_provider
{
    readonly ObserverKey _key = new("missing-observer", "store", "namespace", "event-log");
    readonly ObserverState _original = new() { Identifier = "original" };
    IGrainState<ObserverState> _state;
    Exception _error;

    void Establish()
    {
        _state = new GrainState<ObserverState> { State = _original };
        eventStoreStorage.AreObserversInitialized().Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(() => provider.ReadStateAsync("state", GrainId.Create("type", _key.ToString()), _state));

    [Fact] void should_refuse_activation() => _error.ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    [Fact] void should_name_the_requested_observer_despite_the_missing_record() => _error.Message.ShouldContain($"observer '{_key.ObserverId}'");
    [Fact] void should_leave_the_grain_state_unassigned() => _state.State.ShouldBeSame(_original);
    [Fact] void should_log_the_refusal_at_error() => logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Error && ReferenceEquals(call.GetArguments()[3], _error)).ShouldEqual(1);
}
