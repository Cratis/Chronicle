// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// The client half of a registration storm: a re-trigger - a reconnect, or any operation re-running connect after a
/// failed registration - arrives while a registration is still on the wire. It must join the in-flight run rather
/// than send the kernel a second copy of the same registration to queue behind the first.
/// </summary>
public class and_a_second_call_arrives_while_one_is_in_flight : given.an_event_store_with_a_projection_that_cannot_be_built
{
    TaskCompletionSource _kernelGate;
    Task _first;
    Task _second;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _kernelGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _servicesAccessor.Services.EventStores.Ensure(Arg.Any<EnsureEventStore>()).Returns(_ => _kernelGate.Task);
    }

    async Task Because()
    {
        await _projections.Discover();
        _first = _eventStore.RegisterAll();
        _second = _eventStore.RegisterAll();
        _kernelGate.SetResult();
        await Task.WhenAll(_first, _second);
    }

    [Fact] void should_share_the_in_flight_run() => _second.ShouldEqual(_first);
    [Fact] void should_only_send_one_registration_to_the_kernel() => _servicesAccessor.Services.EventStores.Received(1).Ensure(Arg.Any<EnsureEventStore>());
    [Fact] void should_report_the_shared_run_as_successful() => _eventStore.Registration.IsSuccess.ShouldBeTrue();
}
