// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class two_views_with_the_same_name : a_rejected_view
{
    [ViewOf<Person>("People")]
    public record First(Guid Id);

    [ViewOf<Person>("People")]
    public record Second(Guid Id);

    void Because() => _views = Read(typeof(First), typeof(Second));

    [Fact] void should_refuse_the_view() => FailureOf<First>().ShouldBeOfExactType<DuplicateViewName>();
    [Fact] void should_give_the_reason() => FailureOf<First>().Reason.ShouldEqual("DuplicateViewName");
    [Fact] void should_leave_it_out_of_the_definitions() => _views.IsView(typeof(First)).ShouldBeFalse();
}
