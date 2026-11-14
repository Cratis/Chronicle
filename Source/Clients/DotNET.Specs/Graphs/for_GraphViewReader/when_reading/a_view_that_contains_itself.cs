// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_that_contains_itself : a_rejected_view
{
    [ViewOf<Group>]
    public record GroupTree(Guid Id, [Follows<PartOf>(GraphViewDirection.Outgoing)] GroupTree? Parent);

    void Because() => _views = Read(typeof(GroupTree));

    [Fact] void should_refuse_the_view() => FailureOf<GroupTree>().ShouldBeOfExactType<RecursiveView>();
    [Fact] void should_give_the_reason() => FailureOf<GroupTree>().Reason.ShouldEqual("RecursiveView");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(GroupTree)).ShouldBeFalse();
}
