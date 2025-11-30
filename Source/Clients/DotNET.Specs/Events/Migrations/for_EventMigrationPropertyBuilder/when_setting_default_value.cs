// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_setting_default_value : Specification
{
    EventMigrationPropertyBuilder _builder;
    string _result;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _result = _builder.DefaultValue(42);

    [Fact] void should_return_expression_key() => _result.ShouldContain("__expr_");

    [Fact] void should_have_property_in_properties_collection() => _builder.Properties.ContainsKey(_result).ShouldBeTrue();

    [Fact] void should_have_default_value_expression() => _builder.Properties[_result].ToString().ShouldContain("$defaultValue");
}
