// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor;

public class when_combining_properties : Specification
{
    EventMigrationPropertyBuilder _inner;
    EventMigrationPropertyBuilderFor<SourceEvent, TargetEvent> _builder;

    void Establish()
    {
        _inner = new EventMigrationPropertyBuilder();
        _builder = new EventMigrationPropertyBuilderFor<SourceEvent, TargetEvent>(_inner);
    }

    void Because() => _builder.Combine(t => t.FullName, PropertySeparator.Space, s => s.FirstName, s => s.LastName);

    [Fact] void should_have_property_keyed_by_target_name() => _inner.Properties.ContainsKey("FullName").ShouldBeTrue();

    [Fact] void should_have_combine_expression() => _inner.Properties["FullName"].ToString().ShouldContain("$combine");
}
