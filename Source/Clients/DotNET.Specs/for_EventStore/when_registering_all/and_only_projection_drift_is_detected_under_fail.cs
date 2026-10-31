// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Discovery;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_only_projection_drift_is_detected_under_fail : given.an_event_store_with_identity_drift
{
    void Establish()
    {
        SetField("_identityDriftPolicy", ArtifactIdentityDriftPolicy.Fail);
        _servicesAccessor.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>()).Returns([
            new ObserverInformation { Id = "Original.BuildableProjection", Type = ObserverType.Projection, Owner = ObserverOwner.Kernel, EventSequenceId = "event-log" }
        ]);
        _servicesAccessor.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>())
            .Returns(new ObserverInformation { IsSubscribed = true });
    }

    async Task Because() => _exception = await Catch.Exception(_eventStore.RegisterAll);

    [Fact] void should_continue_registration() => _exception.ShouldBeNull();
    [Fact] void should_publish_no_failure() => _eventStore.Registration.Failure.ShouldBeNull();
    [Fact] void should_retain_the_information_level_finding() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Projection, "Original.BuildableProjection", typeof(BuildableProjection).FullName!));
    [Fact] void should_register_event_types() => _eventStore.EventTypes.Received(1).Register();
}
