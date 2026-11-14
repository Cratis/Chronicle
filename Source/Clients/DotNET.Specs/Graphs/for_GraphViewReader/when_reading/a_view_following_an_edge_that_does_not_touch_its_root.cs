// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_following_an_edge_that_does_not_touch_its_root : a_rejected_view
{
    [ViewOf<Place>]
    public record Elsewhere(Guid Id, [Follows<MemberOf>] IReadOnlyList<GroupSummary> Groups);

    void Because() => _views = Read(typeof(Elsewhere), typeof(GroupSummary));

    [Fact] void should_refuse_the_view() => FailureOf<Elsewhere>().ShouldBeOfExactType<EdgeNotIncidentOnRoot>();
    [Fact] void should_give_the_reason() => FailureOf<Elsewhere>().Reason.ShouldEqual("EdgeNotIncidentOnRoot");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Elsewhere)).ShouldBeFalse();
}
