// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting;

public class and_the_candidate_is_disconnected : given.a_detector
{
    async Task Because() => _result = await _detector.Detect(_current);

    [Fact] void should_report_the_rename() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail"));
    [Fact] void should_confirm_in_the_target_namespace_and_sequence() => _services.Services.Observers.Received(1).GetObserverInformation(Arg.Is<GetObserverInformationRequest>(_ => _.EventStore == "Testing" && _.Namespace == "default" && _.EventSequenceId == "event-log" && _.ObserverId == "Original.SendEmail"), Arg.Any<CallContext>());
}
