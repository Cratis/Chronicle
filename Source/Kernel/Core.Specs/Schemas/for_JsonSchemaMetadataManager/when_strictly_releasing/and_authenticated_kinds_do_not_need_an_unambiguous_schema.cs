// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_authenticated_kinds_do_not_need_an_unambiguous_schema : given.real_pii_protection
{
    JsonObject _display;
    StatusedRelease _statused;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "alternatives":{"oneOf":[{"type":"number"},{"type":"string"}]},
              "unspecified":{},
              "ambiguous":{"type":["number","string"]}
            }}
            """);
        foreach (var property in _schema.Properties.Values) Mark(property);
        _plaintext = new JsonObject { ["alternatives"] = "42", ["unspecified"] = "42", ["ambiguous"] = 42 };
        await Protect();
    }

    async Task Because()
    {
        _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);
        _display = await _manager.Release("store", "tenant", _schema, "subject", _protected);
        _statused = await _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", _protected);
    }

    [Fact] void should_preserve_the_string_despite_alternative_types() => _result["alternatives"].GetValue<string>().ShouldEqual("42");
    [Fact] void should_preserve_the_string_despite_no_type() => _result["unspecified"].GetValue<string>().ShouldEqual("42");
    [Fact] void should_preserve_the_number_despite_ambiguous_type() => _result["ambiguous"].GetValue<int>().ShouldEqual(42);
    [Fact] void should_preserve_all_display_kinds() => JsonNode.DeepEquals(_display, _plaintext).ShouldBeTrue();
    [Fact] void should_preserve_all_statused_kinds() => JsonNode.DeepEquals(_statused.Content, _plaintext).ShouldBeTrue();
    [Fact] void should_report_decrypted_boundaries() => _statused.Statuses.Values.All(status => status == ProtectedValueReleaseStatus.Decrypted).ShouldBeTrue();
}
