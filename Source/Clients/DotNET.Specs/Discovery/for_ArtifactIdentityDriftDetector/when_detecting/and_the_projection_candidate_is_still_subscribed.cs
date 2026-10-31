// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting;

public class and_the_projection_candidate_is_still_subscribed : given.a_detector
{
    void Establish()
    {
        _services.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>())
            .Returns([new ObserverInformation { Id = "Original.SendEmail", EventSequenceId = "event-log", Type = ObserverType.Projection, Owner = ObserverOwner.Kernel }]);
        _services.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>())
            .Returns(new ObserverInformation { IsSubscribed = true });
        _current = _current with { Artifacts = _current.Artifacts.SetItem(0, _current.Artifacts[0] with { Kind = ArtifactKind.Projection }) };
    }

    async Task Because() => _result = await _detector.Detect(_current);

    [Fact] void should_report_the_projection_rename() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Projection, "Original.SendEmail", "Renamed.SendEmail"));
    [Fact] void should_not_confirm_its_subscription() => _services.Services.Observers.DidNotReceive().GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>());
}
