// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns;

public class for_a_context : given.a_patterns_client
{
    Contract.GetPatternsRequest _request;
    IEnumerable<BehaviorPattern> _result;

    void Establish()
    {
        _patterns
            .GetPatterns(Arg.Do<Contract.GetPatternsRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns(
            [
                new Contract.Pattern
                {
                    GroupingKey = "user-42",
                    Facets = new Dictionary<string, string> { { "Day", "Monday" }, { "TimeBucket", "Morning" } },
                    Confidence = 0.9d,
                    Support = 0.4d,
                    Occurrences = 12,
                    Weight = 3.5d,
                    FirstSeen = DateTimeOffset.UnixEpoch,
                    LastSeen = DateTimeOffset.UnixEpoch
                }
            ]);
    }

    async Task Because() => _result = await _client.GetPatterns(
        "user-42",
        FacetSet.Empty.With(FacetName.Day, "Monday").With(FacetName.TimeBucket, "Morning"),
        new PatternConfidence(0.6d),
        5);

    [Fact] void should_ask_for_the_event_store() => _request.EventStore.ShouldEqual(EventStore);
    [Fact] void should_ask_for_the_namespace() => _request.Namespace.ShouldEqual(Namespace);
    [Fact] void should_ask_within_the_scope() => _request.GroupingKey.ShouldEqual("user-42");
    [Fact] void should_pass_the_context() => _request.Context.Count.ShouldEqual(2);
    [Fact] void should_pass_the_day_from_the_context() => _request.Context["Day"].ShouldEqual("Monday");
    [Fact] void should_pass_the_minimum_confidence() => _request.MinimumConfidence.ShouldEqual(0.6d);
    [Fact] void should_pass_the_result_limit() => _request.MaximumResults.ShouldEqual(5);

    [Fact] void should_return_the_pattern() => _result.Count().ShouldEqual(1);
    [Fact] void should_carry_the_scope() => _result.Single().GroupingKey.Value.ShouldEqual("user-42");
    [Fact] void should_carry_the_facets() => _result.Single().Facets.Specificity.ShouldEqual(2);
    [Fact] void should_carry_the_day() => _result.Single().Facets.ValueOf(FacetName.Day).Value.ShouldEqual("Monday");
    [Fact] void should_carry_the_confidence() => _result.Single().Confidence.Value.ShouldEqual(0.9d);
    [Fact] void should_carry_the_support() => _result.Single().Support.Value.ShouldEqual(0.4d);
    [Fact] void should_carry_the_occurrences() => _result.Single().Occurrences.Value.ShouldEqual(12L);
    [Fact] void should_carry_the_weight() => _result.Single().Weight.Value.ShouldEqual(3.5d);
}
