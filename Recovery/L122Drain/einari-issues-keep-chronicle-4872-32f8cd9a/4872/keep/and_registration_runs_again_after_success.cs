// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_registration_runs_again_after_success : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _inFlightOutcome;
    TaskCompletionSource<CommandResult> _kernelGate;
    TaskCompletionSource _registrationStarted;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _kernelGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _registrationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _servicesAccessor.Services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            _registrationStarted.TrySetResult();
            return _kernelGate.Task;
        });
        var registration = _eventStore.RegisterAll();
        await _registrationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _inFlightOutcome = _eventStore.Registration;
        _kernelGate.SetResult(CommandResult.Success(Guid.NewGuid()));
        await registration;
    }

    [Fact] void should_not_report_the_previous_success_while_registering() => _inFlightOutcome.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_registration_has_not_completed() => _inFlightOutcome.HasRun.ShouldBeFalse();
    [Fact] void should_report_success_after_registration_completes() => _eventStore.Registration.IsSuccess.ShouldBeTrue();
}
