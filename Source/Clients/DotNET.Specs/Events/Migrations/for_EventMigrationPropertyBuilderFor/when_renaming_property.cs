// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor;

public class when_renaming_property : Specification
{
    EventMigrationPropertyBuilder _inner;
    EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent> _builder;

    void Establish()
    {
        _inner = new EventMigrationPropertyBuilder();
        _builder = new EventMigrationPropertyBuilderFor<TargetEvent, SourceEvent>(_inner);
    }

    void Because() => _builder.RenamedFrom(t => t.FirstName, s => s.FullName);

    [Fact] void should_have_property_keyed_by_target_name() => _inner.Properties.ContainsKey("FirstName").ShouldBeTrue();

    [Fact] void should_have_rename_expression() => _inner.Properties["FirstName"].ToString().ShouldContain("$rename");
}
