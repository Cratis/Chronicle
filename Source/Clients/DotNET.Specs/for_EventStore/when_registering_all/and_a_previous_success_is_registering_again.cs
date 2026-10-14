// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_a_previous_success_is_registering_again : given.an_event_store_with_a_projection_that_cannot_be_built
{
    TaskCompletionSource<CommandResult> _kernelGate;
    RegistrationOutcome _during;

    async Task Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _kernelGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _servicesAccessor.Services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ => _kernelGate.Task);
    }

    async Task Because()
    {
        var run = _eventStore.RegisterAll();
        _during = _eventStore.Registration;
        _kernelGate.SetResult(CommandResult.Success(Guid.NewGuid()));
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_mark_registration_before_returning_the_task() => _during.IsRegistering.ShouldBeTrue();
    [Fact] void should_preserve_the_previous_success() => _during.IsSuccess.ShouldBeTrue();
    [Fact] void should_clear_registering_after_success() => _eventStore.Registration.IsRegistering.ShouldBeFalse();
}
