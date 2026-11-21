// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;
using Cratis.Chronicle.Projections.Graphs;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_following_an_edge_the_graph_does_not_declare : a_rejected_view
{
    [Edge("WATCHES")]
    public record Watches([Source<Person>] Guid FromId, [Target<Person>] Guid ToId);

    [ViewOf<Person>]
    public record Following(Guid Id, [Follows<Watches>(GraphViewDirection.Outgoing)] IReadOnlyList<GroupSummary> Followed);

    void Because() => _views = Read(typeof(Following), typeof(GroupSummary));

    [Fact] void should_refuse_the_view() => FailureOf<Following>().ShouldBeOfExactType<UnknownEdge>();
    [Fact] void should_give_the_reason() => FailureOf<Following>().Reason.ShouldEqual("UnknownEdge");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Following)).ShouldBeFalse();
}
