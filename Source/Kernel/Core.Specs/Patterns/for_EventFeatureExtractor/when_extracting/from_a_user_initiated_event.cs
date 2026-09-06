// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_a_user_initiated_event : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(
        causedBy: new Identity("user-42", "Ada"),
        causation: [new Causation(Occurred, "ApproveExpenseReport", new Dictionary<string, string>())],
        eventSourceType: new EventSourceType("ExpenseReport")));

    [Fact] void should_group_by_the_user() => _result.GroupingKey.Value.ShouldEqual("user-42");
    [Fact] void should_recognize_a_user() => _result.InitiatorType.ShouldEqual(InitiatorType.User);
    [Fact] void should_carry_the_initiator() => _result.InitiatorId.Value.ShouldEqual("user-42");
    [Fact] void should_not_carry_an_on_behalf_of() => _result.OnBehalfOf.ShouldEqual(FacetValue.Unspecified);
    [Fact] void should_take_the_command_type_from_the_causation() => _result.CommandType.Value.ShouldEqual("ApproveExpenseReport");
    [Fact] void should_not_carry_a_command_a_level_up() => _result.CausedByCommand.ShouldEqual(FacetValue.Unspecified);
    [Fact] void should_carry_the_aggregate_type() => _result.AggregateType.Value.ShouldEqual("ExpenseReport");
    [Fact] void should_carry_the_correlation() => _result.CorrelationRootId.Value.ShouldEqual(Correlation.ToString());
}
