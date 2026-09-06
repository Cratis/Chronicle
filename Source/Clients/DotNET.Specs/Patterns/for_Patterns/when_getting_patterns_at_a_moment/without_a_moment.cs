// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns_at_a_moment;

/// <summary>
/// "What does this person usually do right now" is the question an application actually has, so now is the default.
/// </summary>
public class without_a_moment : given.a_patterns_client
{
    Contract.GetPatternsRequest _request;
    DateTimeOffset _asked;

    void Establish() =>
        _patterns
            .GetUsualActions(Arg.Do<Contract.GetPatternsRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns([]);

    async Task Because()
    {
        _asked = DateTimeOffset.Now;
        await _client.GetPatternsAt("user-42");
    }

    [Fact] void should_constrain_today() => _request.Context["Day"].ShouldEqual(_asked.DayOfWeek.ToString());
    [Fact] void should_constrain_the_current_part_of_the_day() => _request.Context["TimeBucket"].ShouldEqual(_asked.ToTimeBucket().ToString());
}
