// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_derived_source_labels : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_treat_the_label_and_event_target_as_the_same_source(bool derivedTarget)
    {
        EventSourceId source = new SourceId("book");
        EventSourceId label = "book";
        var batch = new EventsWithConcurrencyScopes(
            [new(derivedTarget ? source : label, new object())],
            [new(derivedTarget ? label : source, ConcurrencyScope.NotSet)],
            true);
        Assert.Same(derivedTarget ? label : source, batch.ConcurrencyScopes.Keys.Single());
        Assert.Same(derivedTarget ? source : label, batch.Events.Single().EventSourceId);
    }

    [Fact]
    public void should_refuse_duplicate_wire_labels_even_when_the_clr_types_differ()
    {
        var error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes(
            [new("book", new object())],
            [new("book", ConcurrencyScope.None), new(new SourceId("book"), ConcurrencyScope.None)],
            true));
        error.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
    }

    [Fact]
    public void should_keep_distinct_legacy_labels_outside_read_protection()
    {
        EventSourceId source = new SourceId("book");
        EventSourceId label = "book";
        var scope = new ConcurrencyScope(0, source);
        var batch = new EventsWithConcurrencyScopes([new(source, new object())], [new(source, scope), new(label, ConcurrencyScope.None)]);

        batch.ConcurrencyScopes.Count.ShouldEqual(2);
        Assert.Same(source, batch.Events.Single().EventSourceId);
        Assert.Same(source, batch.ConcurrencyScopes.Keys.First());
        Assert.Same(source, batch.ConcurrencyScopes[source].EventSourceId);
    }

    record SourceId(string Value) : EventSourceId(Value);
}
