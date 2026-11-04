// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelChangeBookkeeping.when_declaring_names;

public class and_asking_repeatedly_for_one_schema : Specification
{
    JsonSchema _schema;
    JsonSchema _other;
    FrozenSet<string> _first;
    FrozenSet<string> _second;
    FrozenSet<string> _otherNames;

    void Establish()
    {
        _schema = new JsonSchema();
        _schema.Properties["name"] = new JsonSchemaProperty("name", new System.Text.Json.Nodes.JsonObject { ["type"] = "string" }, _schema);
        _other = new JsonSchema();
    }

    void Because()
    {
        _first = ReadModelChangeBookkeeping.DeclaredNames(_schema);
        _second = ReadModelChangeBookkeeping.DeclaredNames(_schema);
        _otherNames = ReadModelChangeBookkeeping.DeclaredNames(_other);
    }

    [Fact] void should_return_the_same_set_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_contain_the_declared_property() => _first.Contains("name").ShouldBeTrue();
    [Fact] void should_report_declaration_through_the_cache() => ReadModelChangeBookkeeping.DeclaresProperty(_schema, "name").ShouldBeTrue();
    [Fact] void should_not_declare_bookkeeping_that_is_absent() => ReadModelChangeBookkeeping.DeclaresProperty(_schema, WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeFalse();
    [Fact] void should_not_share_sets_between_schemas() => ReferenceEquals(_first, _otherNames).ShouldBeFalse();
}
