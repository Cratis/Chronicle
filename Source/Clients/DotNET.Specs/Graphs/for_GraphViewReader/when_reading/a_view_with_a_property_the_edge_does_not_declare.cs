// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_with_a_property_the_edge_does_not_declare : a_rejected_view
{
    [ViewOf<Group>]
    public record WithNote(Guid Id, [FromEdge] string Note);

    [ViewOf<Person>]
    public record Referrer(Guid Id, [Follows<MemberOf>] IReadOnlyList<WithNote> Groups);

    void Because() => _views = Read(typeof(WithNote), typeof(Referrer));

    [Fact] void should_refuse_the_view() => FailureOf<WithNote>().ShouldBeOfExactType<UnknownEdgeProperty>();
    [Fact] void should_give_the_reason() => FailureOf<WithNote>().Reason.ShouldEqual("UnknownEdgeProperty");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(WithNote)).ShouldBeFalse();
}
