// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_reading_edge_properties_reached_along_two_edges : a_rejected_view
{
    [ViewOf<Group>]
    public record Shared(Guid Id, [FromEdge] DateTimeOffset Since);

    [ViewOf<Person>]
    public record ByMembership(Guid Id, [Follows<MemberOf>] IReadOnlyList<Shared> Groups);

    [ViewOf<Person>]
    public record ByRole(Guid Id, [Follows<HasRole>] IReadOnlyList<Shared> Groups);

    void Because() => _views = Read(typeof(Shared), typeof(ByMembership), typeof(ByRole));

    [Fact] void should_refuse_the_view() => FailureOf<Shared>().ShouldBeOfExactType<EdgePropertyViewSharedAcrossEdges>();
    [Fact] void should_give_the_reason() => FailureOf<Shared>().Reason.ShouldEqual("EdgePropertyViewSharedAcrossEdges");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Shared)).ShouldBeFalse();
}
