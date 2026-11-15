// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class a_view_that_is_also_a_node : a_rejected_view
{
    [ViewOf<Person>]
    [Node]
    public record Both([Key] Guid Id);

    void Because() => _views = Read(typeof(Both));

    [Fact] void should_refuse_the_view() => FailureOf<Both>().ShouldBeOfExactType<ViewIsAlsoSchema>();
    [Fact] void should_give_the_reason() => FailureOf<Both>().Reason.ShouldEqual("ViewIsAlsoSchema");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(Both)).ShouldBeFalse();
}
