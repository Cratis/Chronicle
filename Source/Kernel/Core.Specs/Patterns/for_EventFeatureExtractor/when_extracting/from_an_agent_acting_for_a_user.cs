// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

/// <summary>
/// An agent acting for somebody contributes to that person's behavior, not to its own - otherwise the same habit
/// would be split across every agent that happened to carry it out.
/// </summary>
public class from_an_agent_acting_for_a_user : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(
        causedBy: new Identity("agent-7", "Assistant", OnBehalfOf: new Identity("user-42", "Ada"))));

    [Fact] void should_group_by_the_person_behind_the_agent() => _result.GroupingKey.Value.ShouldEqual("user-42");
    [Fact] void should_recognize_an_agent() => _result.InitiatorType.ShouldEqual(InitiatorType.Agent);
    [Fact] void should_carry_the_agent_as_the_initiator() => _result.InitiatorId.Value.ShouldEqual("agent-7");
    [Fact] void should_carry_who_it_acted_for() => _result.OnBehalfOf.Value.ShouldEqual("user-42");
}
