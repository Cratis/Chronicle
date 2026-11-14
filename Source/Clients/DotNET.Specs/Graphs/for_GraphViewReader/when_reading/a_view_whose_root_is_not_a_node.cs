// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_whose_root_is_not_a_node : a_rejected_view
{
    public record NotANode(Guid Id);

    [ViewOf<NotANode>]
    public record Rootless(Guid Id);

    void Because() => _views = Read(typeof(Rootless));

    [Fact] void should_refuse_the_view() => FailureOf<Rootless>().ShouldBeOfExactType<ViewRootNotANode>();
    [Fact] void should_give_the_reason() => FailureOf<Rootless>().Reason.ShouldEqual("ViewRootNotANode");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Rootless)).ShouldBeFalse();
}
