// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_state_with_a_type_set_control : given.the_provider
{
    readonly ObserverKey _key = new("observer", "store", "namespace", "event-log");
    readonly IGrainState<ObserverState> _state = new GrainState<ObserverState> { State = new() };
    Exception _error;

    void Establish() => observerStateStorage.Get(_key.ObserverId).Returns(new ObserverState
    {
        TypeSetControl = new(new(1), new("reactor", ObserverFilters.None), new Dictionary<Concepts.Events.EventTypeId, ObserverTypeSetMember>(), [])
    });

    async Task Because() => _error = await Catch.Exception(() => provider.ReadStateAsync("state", GrainId.Create("type", _key.ToString()), _state));

    [Fact] void should_refuse_activation() => _error.ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    [Fact] void should_leave_the_grain_state_unassigned() => _state.State.TypeSetControl.ShouldBeNull();
    [Fact] void should_name_the_requested_observer() => _error.Message.ShouldContain($"observer '{_key.ObserverId}'");
    [Fact] void should_log_the_refusal_at_error() => logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Error && ReferenceEquals(call.GetArguments()[3], _error)).ShouldEqual(1);
}
