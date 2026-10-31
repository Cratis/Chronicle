// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Discovery;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_the_drift_check_cannot_reach_the_kernel : given.an_event_store_with_identity_drift
{
    void Establish()
    {
        SetField("_identityDriftPolicy", ArtifactIdentityDriftPolicy.Fail);
        _servicesAccessor.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>())
            .Returns(Task.FromException<IEnumerable<ObserverInformation>>(new KernelUnavailable()));
    }

    async Task Because() => _exception = await Catch.Exception(_eventStore.RegisterAll);

    [Fact] void should_continue_registration() => _exception.ShouldBeNull();
    [Fact] void should_not_invent_a_finding() => _eventStore.Registration.IdentityDrift.ShouldBeEmpty();
    [Fact] void should_register_event_types() => _eventStore.EventTypes.Received(1).Register();

    public class KernelUnavailable() : Exception("Kernel unavailable");
}
