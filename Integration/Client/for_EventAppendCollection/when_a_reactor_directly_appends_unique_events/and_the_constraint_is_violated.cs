// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventAppendCollection.when_a_reactor_directly_appends_unique_events.and_the_constraint_is_satisfied.context;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.when_a_reactor_directly_appends_unique_events;

[Collection(ChronicleCollection.Name)]
public class and_the_constraint_is_violated(context context) : Given<context>(context)
{
    AppendedEventWithResult SecondFollowUp => Context.AppendedEventsCollector.All.Where(e => e.Event.Content is ADirectUniqueFollowUpEvent).Skip(1).First();

    [Fact] void should_not_be_successful() => SecondFollowUp.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_constraint_violations() => SecondFollowUp.Result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_have_one_constraint_violation() => SecondFollowUp.Result.ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_have_appended_the_follow_up_event_attempt() => SecondFollowUp.Event.Content.ShouldBeOfExactType<ADirectUniqueFollowUpEvent>();
}
