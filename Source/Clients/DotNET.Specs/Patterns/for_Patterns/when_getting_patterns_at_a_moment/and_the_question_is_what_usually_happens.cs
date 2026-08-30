// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns_at_a_moment;

/// <summary>
/// "What does this person usually do right now" wants the command, not a restatement of the moment. Routed at the
/// describing query instead, it can only ever come back with the day and the time it was handed - which is what it
/// did before the answering query existed.
/// </summary>
public class and_the_question_is_what_usually_happens : given.a_patterns_client
{
    static readonly DateTimeOffset _moment = new(2026, 8, 27, 9, 30, 0, TimeSpan.Zero);

    void Establish()
    {
        _patterns.GetUsualActions(Arg.Any<Contract.GetPatternsRequest>(), Arg.Any<CallContext>()).Returns([]);
        _patterns.GetPatterns(Arg.Any<Contract.GetPatternsRequest>(), Arg.Any<CallContext>()).Returns([]);
    }

    async Task Because() => await _client.GetPatternsAt("user-42", _moment);

    [Fact] async Task should_ask_what_usually_happens() =>
        await _patterns.Received(1).GetUsualActions(Arg.Any<Contract.GetPatternsRequest>(), Arg.Any<CallContext>());

    [Fact] async Task should_not_ask_which_patterns_describe_the_moment() =>
        await _patterns.DidNotReceive().GetPatterns(Arg.Any<Contract.GetPatternsRequest>(), Arg.Any<CallContext>());
}
