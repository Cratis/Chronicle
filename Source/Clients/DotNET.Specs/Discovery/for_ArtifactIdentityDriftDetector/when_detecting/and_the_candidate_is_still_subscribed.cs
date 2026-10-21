// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting;

public class and_the_candidate_is_still_subscribed : given.a_detector
{
    void Establish() => _services.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>()).Returns(new ObserverInformation { IsSubscribed = true });

    async Task Because() => _result = await _detector.Detect(_current);

    [Fact] void should_not_block_registration() => _result.ShouldBeEmpty();
}
