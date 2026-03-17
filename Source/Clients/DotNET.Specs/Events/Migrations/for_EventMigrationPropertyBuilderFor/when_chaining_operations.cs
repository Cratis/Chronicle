// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor;

public class when_chaining_operations : Specification
{
    EventMigrationPropertyBuilder _inner;
    EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent> _builder;

    void Establish()
    {
        _inner = new EventMigrationPropertyBuilder();
        _builder = new EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent>(_inner);
    }

    void Because() => _builder
        .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
        .Split(t => t.LastName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second)
        .DefaultValue(t => t.Age, 0);

    [Fact] void should_have_three_properties() => _inner.Properties.Count.ShouldEqual(3);

    [Fact] void should_have_first_name() => _inner.Properties.ContainsKey("FirstName").ShouldBeTrue();

    [Fact] void should_have_last_name() => _inner.Properties.ContainsKey("LastName").ShouldBeTrue();

    [Fact] void should_have_age() => _inner.Properties.ContainsKey("Age").ShouldBeTrue();
}
