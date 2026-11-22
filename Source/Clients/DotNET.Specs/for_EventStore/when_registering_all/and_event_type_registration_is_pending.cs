// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_event_type_registration_is_pending : given.an_event_store_with_a_projection_that_cannot_be_built
{
    readonly TaskCompletionSource _eventTypesStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _acceptEventTypes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _readModelsStartedBeforeAcceptance;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.EventTypes.Register().Returns(_ =>
        {
            _eventTypesStarted.SetResult();
            return _acceptEventTypes.Task;
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        var registration = _eventStore.RegisterAll();
        await _eventTypesStarted.Task;
        _readModelsStartedBeforeAcceptance = _eventStore.ReadModels.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Register");
        _acceptEventTypes.SetResult();
        await registration;
    }

    [Fact] void should_not_start_read_models_while_event_types_are_pending() => _readModelsStartedBeforeAcceptance.ShouldBeFalse();
    [Fact] async Task should_register_read_models_after_acceptance() => await _eventStore.ReadModels.Received(1).Register();
    [Fact] void should_complete_registration_successfully() => _eventStore.Registration.IsSuccess.ShouldBeTrue();
}
