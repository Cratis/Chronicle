// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Grpc.Core;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_read_model_registration_is_refused : given.an_event_store_with_a_projection_that_cannot_be_built
{
    readonly RpcException _failure = new(new Status(StatusCode.FailedPrecondition, "Read model 'model' uses 'stored', not 'requested'. Use UpdateDefinition explicitly."));
    Exception _thrown;

    void Establish()
    {
        _eventStore.ReadModels.Register().Returns(Task.FromException(_failure));
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.FromHours(1), TimeSpan.FromHours(1)));
    }

    async Task Because()
    {
        _thrown = await Catch.Exception(_eventStore.RegisterAll);
        _eventStore.StopBackgroundRegistrationRetry();
    }

    [Fact] void should_surface_the_actionable_refusal() => _thrown.ShouldEqual(_failure);
    [Fact] void should_keep_the_original_error_in_the_outcome() => _eventStore.Registration.Failure.ShouldEqual(_failure);
    [Fact] void should_not_report_success() => _eventStore.Registration.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_start_constraints() => await _eventStore.Constraints.DidNotReceive().Register();
    [Fact] async Task should_not_start_reactors() => await _eventStore.Reactors.DidNotReceive().Register();
    [Fact] async Task should_not_start_reducers() => await _eventStore.Reducers.DidNotReceive().Register();
    [Fact] async Task should_not_start_seeding() => await _eventStore.Seeding.DidNotReceive().Register();
}
