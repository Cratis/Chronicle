// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Discovery;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_identity_drift_is_detected_under_fail : given.an_event_store_with_identity_drift
{
    void Establish()
    {
        SetField("_identityDriftPolicy", ArtifactIdentityDriftPolicy.Fail);
        _registrationRetry.MaxAttempts = 5;
    }

    async Task Because() => _exception = await Catch.Exception(_eventStore.RegisterAll);

    [Fact] void should_fail_with_the_identity_change() => _exception.ShouldBeOfExactType<ArtifactIdentityDriftDetected>();
    [Fact] void should_publish_the_failure() => _eventStore.Registration.Failure.ShouldEqual(_exception);
    [Fact] void should_report_the_finding() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(ExpectedDrift);
    [Fact] void should_not_ensure_the_event_store() => _servicesAccessor.Services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_register_event_types() => _eventStore.EventTypes.DidNotReceive().Register();
    [Fact] void should_not_retry_in_the_background() => _eventStore.PendingBackgroundRegistrationRetry.ShouldBeNull();
}
