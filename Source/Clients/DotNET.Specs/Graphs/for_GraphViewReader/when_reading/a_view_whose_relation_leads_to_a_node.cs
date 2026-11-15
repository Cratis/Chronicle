// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_whose_relation_leads_to_a_node : a_rejected_view
{
    [ViewOf<Person>]
    public record ToNode(Guid Id, [Follows<MemberOf>] IReadOnlyList<Group> Groups);

    void Because() => _views = Read(typeof(ToNode));

    [Fact] void should_refuse_the_view() => FailureOf<ToNode>().ShouldBeOfExactType<RelationTargetNotAView>();
    [Fact] void should_give_the_reason() => FailureOf<ToNode>().Reason.ShouldEqual("RelationTargetNotAView");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(ToNode)).ShouldBeFalse();
}
