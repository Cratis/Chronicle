// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_following_a_self_edge_without_a_direction : a_rejected_view
{
    [ViewOf<Group>]
    public record Undirected(Guid Id, [Follows<PartOf>] GroupSummary? Parent);

    void Because() => _views = Read(typeof(Undirected), typeof(GroupSummary));

    [Fact] void should_refuse_the_view() => FailureOf<Undirected>().ShouldBeOfExactType<AmbiguousDirection>();
    [Fact] void should_give_the_reason() => FailureOf<Undirected>().Reason.ShouldEqual("AmbiguousDirection");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Undirected)).ShouldBeFalse();
}
