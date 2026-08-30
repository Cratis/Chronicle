// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns_at_a_moment;

/// <summary>
/// The moment below is a Thursday, half past nine in the morning.
/// </summary>
public class for_a_given_moment : given.a_patterns_client
{
    static readonly DateTimeOffset _moment = new(2026, 8, 27, 9, 30, 0, TimeSpan.Zero);

    Contract.GetPatternsRequest _request;

    void Establish() =>
        _patterns
            .GetPatterns(Arg.Do<Contract.GetPatternsRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns([]);

    async Task Because() => await _client.GetPatternsAt("user-42", _moment);

    [Fact] void should_ask_within_the_scope() => _request.GroupingKey.ShouldEqual("user-42");
    [Fact] void should_constrain_the_day_of_the_moment() => _request.Context["Day"].ShouldEqual("Thursday");
    [Fact] void should_constrain_the_part_of_the_day() => _request.Context["TimeBucket"].ShouldEqual("Morning");
    [Fact] void should_constrain_nothing_else() => _request.Context.Count.ShouldEqual(2);
}
