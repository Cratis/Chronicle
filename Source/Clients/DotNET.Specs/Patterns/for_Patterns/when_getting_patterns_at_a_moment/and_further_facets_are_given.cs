// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns_at_a_moment;

/// <summary>
/// Asking about a moment and asking about the kind of work are the same question, so the moment builds on whatever
/// the caller already wanted to constrain rather than replacing it.
/// </summary>
public class and_further_facets_are_given : given.a_patterns_client
{
    static readonly DateTimeOffset _moment = new(2026, 8, 27, 9, 30, 0, TimeSpan.Zero);

    Contract.GetPatternsRequest _request;

    void Establish() =>
        _patterns
            .GetUsualActions(Arg.Do<Contract.GetPatternsRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns([]);

    async Task Because() => await _client.GetPatternsAt(
        "user-42",
        _moment,
        FacetSet.Empty.With(FacetName.AggregateType, "Invoice"));

    [Fact] void should_keep_the_facet_the_caller_gave() => _request.Context["AggregateType"].ShouldEqual("Invoice");
    [Fact] void should_add_the_day_of_the_moment() => _request.Context["Day"].ShouldEqual("Thursday");
    [Fact] void should_add_the_part_of_the_day() => _request.Context["TimeBucket"].ShouldEqual("Morning");
}
