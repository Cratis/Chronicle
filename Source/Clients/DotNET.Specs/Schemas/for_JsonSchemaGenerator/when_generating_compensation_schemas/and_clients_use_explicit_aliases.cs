// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_clients_use_explicit_aliases : given.registry_bindings
{
    JsonSchema _firstPrecise;
    JsonSchema _secondPrecise;
    JsonSchema _firstLegacy;
    JsonSchema _secondLegacy;
    JsonSchema _firstAgain;
    JsonSchema _secondAgain;

    void Because()
    {
        _firstPrecise = _generator.Generate(typeof(Compensation), _first);
        _secondPrecise = _generator.Generate(typeof(Compensation), _second);
        _secondLegacy = _generator.GenerateLegacyEventType(typeof(Compensation), _second);
        _firstLegacy = _generator.GenerateLegacyEventType(typeof(Compensation), _first);
        _secondAgain = _generator.Generate(typeof(Compensation), _second);
        _firstAgain = _generator.Generate(typeof(Compensation), _first);
    }

    [Fact] void should_bind_precise_schema_to_the_first_client() => _firstPrecise.GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_bind_precise_schema_to_the_second_client() => _secondPrecise.GetCompensationFor().ShouldEqual("second-alias");
    [Fact] void should_bind_legacy_schema_to_the_first_client() => _firstLegacy.GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_bind_legacy_schema_to_the_second_client() => _secondLegacy.GetCompensationFor().ShouldEqual("second-alias");
    [Fact] void should_preserve_the_first_identity_on_cache_reuse() => _firstAgain.GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_preserve_the_second_identity_on_cache_reuse() => _secondAgain.GetCompensationFor().ShouldEqual("second-alias");
    [Fact] void should_not_share_decorated_nodes_between_clients() => ReferenceEquals(_firstPrecise, _secondPrecise).ShouldBeFalse();
    [Fact] void should_preserve_precise_default_property_shape() => JsonNode.Parse(_firstPrecise.ToJson())!["properties"]!["Value"]!["type"]!.GetValue<string>().ShouldEqual("string");
}
