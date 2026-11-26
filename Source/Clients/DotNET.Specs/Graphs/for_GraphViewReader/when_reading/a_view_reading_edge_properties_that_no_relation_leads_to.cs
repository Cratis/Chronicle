// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_reading_edge_properties_that_no_relation_leads_to : a_rejected_view
{
    [ViewOf<Group>]
    public record Orphan(Guid Id, [FromEdge] DateTimeOffset Since);

    void Because() => _views = Read(typeof(Orphan));

    [Fact] void should_refuse_the_view() => FailureOf<Orphan>().ShouldBeOfExactType<EdgePropertyViewNotUsedAsTarget>();
    [Fact] void should_give_the_reason() => FailureOf<Orphan>().Reason.ShouldEqual("EdgePropertyViewNotUsedAsTarget");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Orphan)).ShouldBeFalse();
}
