// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionRegistrationFailed;

/// <summary>
/// Registration attributes a batch failure to the definition that produced it, and the attribution is what a
/// caller reads to learn which projection is at fault. It never arrives bare: the batch registers its definitions
/// through Task.WhenAll, which aggregates, and the grain call wraps whatever comes back.
/// </summary>
/// <remarks>
/// The specifications covering the attribution all handed it over unwrapped, so the whole reason the search
/// recurses - that by the time a caller sees it the attribution is nested arbitrarily deep - went unpinned.
/// Removing the recursion left every one of them green while the production path lost its attribution entirely
/// and fell back to naming every identifier in the batch.
/// </remarks>
public class when_the_attribution_is_wrapped : Specification
{
    static readonly Engine.ProjectionDefinitionRegistrationFailed _attribution = new(
        "the-failing-projection",
        new InvalidOperationException("the root cause"));

    [Fact]
    void should_find_it_under_an_aggregate() =>
        Find(new AggregateException(new InvalidOperationException("an unrelated failure"), _attribution)).Value.ShouldEqual("the-failing-projection");

    [Fact]
    void should_find_it_under_an_inner_exception_chain() =>
        Find(new InvalidOperationException("outer", new InvalidOperationException("middle", _attribution))).Value.ShouldEqual("the-failing-projection");

    [Fact]
    void should_find_it_under_an_aggregate_nested_in_an_inner_exception_chain() =>
        Find(new InvalidOperationException("the grain call failed", new AggregateException(_attribution))).Value.ShouldEqual("the-failing-projection");

    [Fact]
    void should_not_find_one_that_is_not_there() =>
        Engine.ProjectionDefinitionRegistrationFailed.TryFindIdentifier(
            new InvalidOperationException("outer", new InvalidOperationException("inner")), out _).ShouldBeFalse();

    [Fact]
    void should_not_find_one_in_nothing() =>
        Engine.ProjectionDefinitionRegistrationFailed.TryFindIdentifier(null, out _).ShouldBeFalse();

    static Concepts.Projections.ProjectionId Find(Exception exception)
    {
        Engine.ProjectionDefinitionRegistrationFailed.TryFindIdentifier(exception, out var identifier).ShouldBeTrue();
        return identifier!;
    }
}
