// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Registrations;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_a_background_retry_carries_warning_drift_under_fail : given.an_event_store_with_identity_drift
{
    void Establish()
    {
        SetField("_identityDriftPolicy", ArtifactIdentityDriftPolicy.Fail);
        SetField("_registration", new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty) { IdentityDrift = [ExpectedDrift] });
    }

    async Task Because()
    {
        var registerAllInBackground = typeof(EventStore).GetMethod("RegisterAllInBackground", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _exception = await Catch.Exception(() => (Task)registerAllInBackground.Invoke(_eventStore, [])!);
    }

    [Fact] void should_fail_with_the_identity_change() => _exception.ShouldBeOfExactType<ArtifactIdentityDriftDetected>();
    [Fact] void should_keep_the_original_findings() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(ExpectedDrift);
    [Fact] void should_not_snapshot_observers_again() => _servicesAccessor.Services.Observers.DidNotReceive().GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_ensure_the_event_store() => _servicesAccessor.Services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_register_event_types() => _eventStore.EventTypes.DidNotReceive().Register();
}
