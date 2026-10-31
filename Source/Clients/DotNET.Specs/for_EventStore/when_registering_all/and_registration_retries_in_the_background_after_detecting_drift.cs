// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_registration_retries_in_the_background_after_detecting_drift : given.an_event_store_with_identity_drift
{
    int _attempts;

    void Establish() => _eventStore.Seeding.Register().Returns(_ => ++_attempts < 3 ? Task.FromException(new KernelUnavailable()) : Task.CompletedTask);

    async Task Because()
    {
        _exception = await Catch.Exception(_eventStore.RegisterAll);
        await _eventStore.PendingBackgroundRegistrationRetry!.WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_surface_the_initial_registration_failure() => _exception.ShouldBeOfExactType<KernelUnavailable>();
    [Fact] void should_recover_after_two_background_attempts() => _attempts.ShouldEqual(3);
    [Fact] void should_publish_the_recovered_outcome() => _eventStore.Registration.Failure.ShouldBeNull();
    [Fact] void should_retain_the_original_findings() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(ExpectedDrift);
    [Fact] void should_snapshot_observers_only_once() => _servicesAccessor.Services.Observers.Received(1).GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>());
    [Fact] void should_snapshot_event_types_only_once() => _servicesAccessor.Services.EventTypes.Received(1).AllEventTypes(Arg.Any<AllEventTypesRequest>(), Arg.Any<CallContext>());
    [Fact] void should_confirm_subscription_only_once() => _servicesAccessor.Services.Observers.Received(1).GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>());

    public class KernelUnavailable() : Exception("Kernel unavailable");
}
