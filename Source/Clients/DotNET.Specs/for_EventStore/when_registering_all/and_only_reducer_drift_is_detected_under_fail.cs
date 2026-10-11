// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_only_reducer_drift_is_detected_under_fail : given.an_event_store_with_identity_drift
{
    void Establish()
    {
        SetField("_identityDriftPolicy", ArtifactIdentityDriftPolicy.Fail);
        var reducer = Substitute.For<IReducerHandler>();
        reducer.Id.Returns(new ReducerId("Renamed.SomeReducer"));
        reducer.ReducerType.Returns(typeof(string));
        reducer.ReadModelType.Returns(typeof(BuildableModel));
        _eventStore.Reducers.GetAllHandlers().Returns([reducer]);
        _servicesAccessor.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>()).Returns([
            new ObserverInformation { Id = "Original.SomeReducer", Type = ObserverType.Reducer, Owner = ObserverOwner.Client, EventSequenceId = "event-log" }
        ]);
    }

    async Task Because() => _exception = await Catch.Exception(_eventStore.RegisterAll);

    [Fact] void should_continue_registration() => _exception.ShouldBeNull();
    [Fact] void should_publish_no_failure() => _eventStore.Registration.Failure.ShouldBeNull();
    [Fact] void should_retain_the_information_level_finding() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reducer, "Original.SomeReducer", "Renamed.SomeReducer"));
    [Fact] void should_register_event_types() => _eventStore.EventTypes.Received(1).Register();
    [Fact] void should_confirm_the_reducer_subscription() => _servicesAccessor.Services.Observers.Received(1).GetObserverInformation(Arg.Is<GetObserverInformationRequest>(_ => _.ObserverId == "Original.SomeReducer"), Arg.Any<CallContext>());
}
