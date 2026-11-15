// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.when_reading;

public class two_views_that_contain_each_other : a_rejected_view
{
    [ViewOf<Group>]
    public record Parent(Guid Id, [Follows<PartOf>(GraphViewDirection.Incoming)] IReadOnlyList<Child> Children);

    [ViewOf<Group>]
    public record Child(Guid Id, [Follows<PartOf>(GraphViewDirection.Outgoing)] Parent? Parent);

    void Because() => _views = Read(typeof(Parent), typeof(Child));

    [Fact] void should_refuse_the_one() => FailureOf<Parent>().Reason.ShouldEqual("RecursiveView");
    [Fact] void should_refuse_the_other() => FailureOf<Child>().Reason.ShouldEqual("RecursiveView");
}
