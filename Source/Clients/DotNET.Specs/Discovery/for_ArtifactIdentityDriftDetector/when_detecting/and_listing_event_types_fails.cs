// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting;

public class and_listing_event_types_fails : given.a_detector
{
    void Establish() => _services.Services.EventTypes.AllEventTypes(Arg.Any<AllEventTypesRequest>(), Arg.Any<CallContext>()).Returns(QueryResult<IEnumerable<EventTypeDetailsResponse>>.Error(Guid.NewGuid(), new KernelUnavailable()));

    async Task Because() => _result = await _detector.Detect(_current);

    [Fact] void should_not_block_registration() => _result.ShouldBeEmpty();
    [Fact] void should_explain_why_the_candidate_was_omitted() => LoggedAt(_logger, LogLevel.Warning).ShouldBeTrue();
}
