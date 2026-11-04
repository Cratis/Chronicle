// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelKeyNames.when_choosing_the_key_name;

public class and_the_schema_spells_it_in_mixed_case : Specification
{
    JsonSchema _schema;
    string _name;
    bool _requiresLayoutResolution;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "iD": { "type": "string" } } }""");

    void Because()
    {
        _name = ReadModelKeyNames.For(_schema);
        _requiresLayoutResolution = ReadModelKeyNames.RequiresLayoutResolution(_schema);
    }

    [Fact] void should_choose_the_expected_name() => _name.ShouldEqual("iD");
    [Fact] void should_require_layout_resolution() => _requiresLayoutResolution.ShouldEqual(true);
}
