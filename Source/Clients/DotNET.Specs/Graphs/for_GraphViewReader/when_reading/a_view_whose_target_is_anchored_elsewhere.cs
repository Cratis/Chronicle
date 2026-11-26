// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_whose_target_is_anchored_elsewhere : a_rejected_view
{
    [ViewOf<Person>]
    public record PersonSummary(Guid Id);

    [ViewOf<Person>]
    public record Mismatched(Guid Id, [Follows<MemberOf>] IReadOnlyList<PersonSummary> Groups);

    void Because() => _views = Read(typeof(Mismatched), typeof(PersonSummary));

    [Fact] void should_refuse_the_view() => FailureOf<Mismatched>().ShouldBeOfExactType<TargetLabelMismatch>();
    [Fact] void should_give_the_reason() => FailureOf<Mismatched>().Reason.ShouldEqual("TargetLabelMismatch");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Mismatched)).ShouldBeFalse();
}
