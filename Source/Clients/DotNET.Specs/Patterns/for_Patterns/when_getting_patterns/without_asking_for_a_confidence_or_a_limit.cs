// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns;

/// <summary>
/// The thresholds are the server's. A default duplicated in the client would silently disagree with the configured
/// one the moment either changed, so an unasked-for confidence or limit goes over the wire as unset.
/// </summary>
public class without_asking_for_a_confidence_or_a_limit : given.a_patterns_client
{
    Contract.MatchingPatternsRequest _request;

    void Establish() =>
        _patterns
            .MatchingPatterns(Arg.Do<Contract.MatchingPatternsRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns(QueryResult<IEnumerable<Contract.BehaviorPatternDetailsResponse>>.Success(Guid.NewGuid(), []));

    async Task Because() => await _client.GetPatterns("user-42", FacetSet.Empty.With(FacetName.Day, "Monday"));

    [Fact] void should_leave_the_minimum_confidence_to_the_server() => _request.MinimumConfidence.ShouldEqual(0d);
    [Fact] void should_leave_the_result_limit_to_the_server() => _request.MaximumResults.ShouldEqual(0);
}
