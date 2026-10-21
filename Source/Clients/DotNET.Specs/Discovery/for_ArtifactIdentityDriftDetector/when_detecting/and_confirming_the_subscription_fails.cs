// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting;

public class and_confirming_the_subscription_fails : given.a_detector
{
    void Establish() => _services.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>()).Returns(Task.FromException<ObserverInformation>(new KernelUnavailable()));

    async Task Because() => _result = await _detector.Detect(_current);

    [Fact] void should_not_block_registration() => _result.ShouldBeEmpty();
    [Fact] void should_explain_why_the_candidate_was_omitted() => LoggedAt(_logger, LogLevel.Debug).ShouldBeTrue();
}
