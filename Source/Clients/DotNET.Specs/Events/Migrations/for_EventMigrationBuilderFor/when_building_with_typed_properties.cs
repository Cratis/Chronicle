// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilderFor;

public class when_building_with_typed_properties : Specification
{
    EventMigrationBuilder _inner;
    EventMigrationBuilderFor<TargetEvent, SourceEvent> _builder;
    JsonObject _result;

    void Establish()
    {
        _inner = new EventMigrationBuilder();
        _builder = new EventMigrationBuilderFor<TargetEvent, SourceEvent>(_inner);
    }

    void Because()
    {
        _builder.Properties(p => p
            .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
            .Split(t => t.LastName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second));
        _result = _inner.ToJson();
    }

    [Fact] void should_have_two_properties() => _result.Count.ShouldEqual(2);

    [Fact] void should_contain_split_expressions() => _result.ToJsonString().ShouldContain("$split");
}
