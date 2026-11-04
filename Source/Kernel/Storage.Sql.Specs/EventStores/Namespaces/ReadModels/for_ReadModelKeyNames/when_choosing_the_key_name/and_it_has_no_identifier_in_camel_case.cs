// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelKeyNames.when_choosing_the_key_name;

public class and_it_has_no_identifier_in_camel_case : Specification
{
    JsonSchema _schema;
    string _name;
    bool _requiresLayoutResolution;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "name": { "type": "string" }, "title": { "type": "string" } } }""");

    void Because()
    {
        _name = ReadModelKeyNames.For(_schema);
        _requiresLayoutResolution = ReadModelKeyNames.RequiresLayoutResolution(_schema);
    }

    [Fact] void should_choose_the_expected_name() => _name.ShouldEqual("id");
    [Fact] void should_not_require_layout_resolution() => _requiresLayoutResolution.ShouldEqual(false);
}
