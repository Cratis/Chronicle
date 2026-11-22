// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_event_type_registration_fails : given.an_event_store_with_a_projection_that_cannot_be_built
{
    readonly Exception _failure = new InvalidOperationException("Event types were refused");
    Exception _thrown;

    void Establish()
    {
        _eventStore.EventTypes.Register().Returns(Task.FromException(_failure));
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.FromHours(1), TimeSpan.FromHours(1)));
    }

    async Task Because()
    {
        _thrown = await Catch.Exception(_eventStore.RegisterAll);
        _eventStore.StopBackgroundRegistrationRetry();
    }

    [Fact] void should_surface_the_event_type_failure() => _thrown.ShouldEqual(_failure);
    [Fact] void should_record_the_failed_registration() => _eventStore.Registration.Failure.ShouldEqual(_failure);
    [Fact] void should_not_report_success() => _eventStore.Registration.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_start_read_model_registration() => await _eventStore.ReadModels.DidNotReceive().Register();
    [Fact] async Task should_not_start_constraints() => await _eventStore.Constraints.DidNotReceive().Register();
}
