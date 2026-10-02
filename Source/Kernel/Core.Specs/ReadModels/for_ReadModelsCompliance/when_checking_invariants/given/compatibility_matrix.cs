// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class compatibility_matrix
{
    const string MarkerText = "\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]";
    static readonly string[] _references = ["definitions_scalar", "defs_scalar", "definitions_object", "defs_object", "properties_scalar", "plain_name", "plain_name_collision", "self", "mutual", "recursive_anyOf", "recursive_oneOf", "items_anyOf", "items_oneOf", "whole_anyOf", "whole_oneOf", "whole_allOf_anyOf", "whole_allOf_oneOf", "whole_root_anyOf", "whole_root_oneOf", "unprotected_anyOf", "unprotected_oneOf"];

    public static TheoryData<string, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, bool>();
            foreach (var erased in new[] { false, true })
            {
                foreach (var shape in _references)
                    foreach (var pii in new[] { false, true }) cells.Add($"reference/{shape}/{pii}", erased);
                foreach (var union in new[] { "anyOf", "oneOf" })
                {
                    foreach (var shape in new[] { "inline", "reference", "allOf", "allOf_reference" })
                        foreach (var marker in new[] { "none", "sibling", "member", "container" }) cells.Add($"legacy/{union}/{shape}/{marker}", erased);
                    foreach (var shape in new[] { "root", "nested", "sibling", "whole" })
                        foreach (var pii in new[] { false, true }.Where(pii => shape != "whole" || pii)) cells.Add($"union/{union}/{shape}/{pii}", erased);
                }
                foreach (var kind in new[] { "unprotected", "member", "reference", "composition", "container", "container_undeclared", "unresolved", "duplicate" }) cells.Add($"restoration/{kind}", erased);
                foreach (var partial in new[] { false, true })
                    foreach (var casing in new[] { false, true }) cells.Add($"subject/{partial}/{casing}", erased);
                cells.Add("dynamic", erased);
                cells.Add("legacy_items", erased);
            }
            foreach (var shape in new[] { "reference", "union", "conflict", "dynamic", "cycle", "tuple", "malformed" }) cells.Add($"append/{shape}", false);
            foreach (var shape in new[] { "self", "mutual" })
                foreach (var pii in new[] { false, true }) cells.Add($"cycle/{shape}/{pii}", false);
            return cells;
        }
    }

    public static (JsonSchema Schema, ExpandoObject State) Create(string cell)
    {
        var parts = cell.Split('/');
        var (root, input) = parts[0] switch
        {
            "reference" => Reference(parts[1], bool.Parse(parts[2])),
            "legacy" => Legacy(parts[1], parts[2], parts[3]),
            "union" => Union(parts[1], parts[2], bool.Parse(parts[3])),
            "restoration" => Restoration(parts[1]),
            "subject" => MissingSubject(bool.Parse(parts[1]), bool.Parse(parts[2])),
            "append" => Append(parts[1]),
            "cycle" => Cycle(parts[1], bool.Parse(parts[2])),
            "legacy_items" => (
                Parse("""{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","compliance":[{"metadataType":"PII","details":""}],"properties":{"name":{"type":"string"},"count":{"type":"integer"}}}}}}"""),
                Parse("""{"items":[{"name":"Alice","count":42}]}""")),
            _ => (
                Parse("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"headers":{"type":"object","additionalProperties":{"type":"string"}},"count":{"type":"integer","if":{"minimum":0},"then":{"maximum":10}}}}"""),
                Parse("""{"name":"personal-value","headers":{"X-Correlation-Id":"public-value"},"count":3}"""))
        };
        var converter = new ExpandoObjectConverter(new TypeFormats());
        return (JsonSchema.FromJson(root.ToJsonString()), converter.ToExpandoObject(input, new JsonSchema()));
    }

    public static string[] LegacyPaths(string cell)
    {
        var parts = cell.Split('/');
        if (parts[0] == "legacy_items") return ["items.0.name", "items.0.count"];
        if (parts[0] != "legacy") return [];
        return parts[3] switch
        {
            "member" or "container" => ["value", "guard"],
            "sibling" => ["guard"],
            _ => []
        };
    }

    static (JsonObject Root, JsonObject Input) Reference(string shape, bool pii)
    {
        var root = Parse("""{"title":"SameEvent","type":"object","properties":{},"$defs":{}}""");
        var leaf = Parse("""{"type":"string"}""");
        if (pii) leaf["compliance"] = Marker();
        var input = Parse("""{"value":"private"}""");
        if (shape == "definitions_scalar" || shape == "defs_scalar" || shape == "definitions_object" || shape == "defs_object" || shape == "properties_scalar" || shape == "plain_name" || shape == "plain_name_collision")
        {
            var definition = leaf;
            if (shape.EndsWith("_object", StringComparison.Ordinal))
            {
                definition = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["name"] = leaf } };
                input["value"] = new JsonObject { ["name"] = "private" };
            }
            var key = shape.StartsWith("definitions", StringComparison.Ordinal) ? "definitions" : "$defs";
            if (shape == "properties_scalar") key = "properties";
            root[key] ??= new JsonObject();
            root[key]!["Person"] = definition;
            var reference = $"#{key}/Person";
            if (shape == "plain_name" || shape == "plain_name_collision")
            {
                root["Person"] = definition.DeepClone();
                reference = "#Person";
                if (shape == "plain_name_collision") root["$defs"]!["foreign"] = Parse("""{"$id":"foreign","$anchor":"Person","type":"string"}""");
            }
            root["properties"]!["value"] = new JsonObject { ["$ref"] = reference };
        }
        else if (shape.StartsWith("unprotected_", StringComparison.Ordinal))
        {
            root[shape[12..]] = JsonNode.Parse("""[{"type":"object","properties":{"value":{"type":"string"}}},{"type":"null"}]""");
            if (pii) root["properties"]!["guard"] = leaf;
            input["guard"] = "private";
            input["extra"] = "retained";
        }
        else if (shape.StartsWith("items_", StringComparison.Ordinal))
        {
            root["properties"]!["value"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = leaf,
                [shape[6..]] = JsonNode.Parse("""[{"type":"array"},{"type":"null"}]""")
            };
            input["value"] = new JsonArray("private", "second");
        }
        else if (shape.StartsWith("whole_", StringComparison.Ordinal))
        {
            var union = shape.EndsWith("anyOf", StringComparison.Ordinal) ? "anyOf" : "oneOf";
            root["$defs"]!["Person"] = new JsonObject { [union] = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"},"child":{"type":"object","properties":{"name":{"type":"string"}}}}},{"type":"null"}]""") };
            var member = new JsonObject { ["$ref"] = "#/$defs/Person" };
            if (shape.Contains("allOf", StringComparison.Ordinal)) member = new JsonObject { ["allOf"] = new JsonArray(member, new JsonObject()) };
            if (pii) (shape.Contains("root", StringComparison.Ordinal) ? root : member)["compliance"] = Marker();
            root["properties"]!["value"] = member;
            input["value"] = JsonNode.Parse("""{"name":"Alice","Name":"Distinct","extra":"retained","nothing":null,"child":{"name":"Child","extra":"nested-retained"}}""");
            if (!pii) input["value"]!.AsObject().Remove("nothing");
        }
        else
        {
            const string ReferencePath = "#/$defs/Person";
            var next = shape == "mutual" ? "#/$defs/Alias" : ReferencePath;
            var person = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["name"] = leaf, ["next"] = new JsonObject { ["$ref"] = next } } };
            root["$defs"]!["Person"] = shape.StartsWith("recursive_", StringComparison.Ordinal)
                ? new JsonObject { [shape[10..]] = new JsonArray(person, new JsonObject { ["type"] = "null" }) }
                : person;
            root["$defs"]!["Alias"] = new JsonObject { ["$ref"] = ReferencePath };
            root["properties"]!["value"] = new JsonObject { ["$ref"] = ReferencePath };
            input["value"] = JsonNode.Parse("""{"name":"private","next":{"name":"second"}}""");
        }
        return (root, input);
    }

    static (JsonObject Root, JsonObject Input) Legacy(string union, string shape, string marker)
    {
        var target = new JsonObject { [union] = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"object","properties":{"other":{"type":"string"}}}]""") };
        var member = shape switch
        {
            "reference" => new JsonObject { ["$ref"] = "#/$defs/value" },
            "allOf" => new JsonObject { ["allOf"] = new JsonArray(target.DeepClone()) },
            "allOf_reference" => new JsonObject { ["allOf"] = new JsonArray(new JsonObject { ["$ref"] = "#/$defs/value" }) },
            _ => (JsonObject)target.DeepClone()
        };
        var root = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["value"] = member }, ["$defs"] = new JsonObject { ["value"] = target } };
        var whole = marker == "member" || marker == "container";
        if (marker == "member") member["compliance"] = Marker();
        if (marker == "container") root["compliance"] = Marker();
        if (whole) member["type"] = "object";
        var input = Parse(whole ? """{"value":{"Name":"Alice","nothing":null,"child":{"Name":"Child"}}}""" : """{"value":{"name":"Alice"}}""");
        if (marker != "none")
        {
            root["properties"]!["guard"] = new JsonObject { ["type"] = "string", ["compliance"] = Marker() };
            input["guard"] = "private";
        }
        return (root, input);
    }

    static (JsonObject Root, JsonObject Input) Union(string union, string shape, bool pii)
    {
        var alternatives = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"null"}]""")!;
        var root = new JsonObject();
        JsonObject input;
        if (shape == "whole")
        {
            alternatives[1] = new JsonObject { ["$ref"] = "#/$defs/nil" };
            root["$defs"] = new JsonObject { ["nil"] = new JsonObject { ["type"] = "null" } };
            root["type"] = "object";
            var member = new JsonObject { ["compliance"] = Marker(), [union] = alternatives };
            root["properties"] = new JsonObject { ["value"] = member };
            input = Parse("""{"value":{"name":"Alice","extra":"retained"}}""");
        }
        else if (shape == "root")
        {
            root[union] = alternatives;
            input = Parse("""{"name":"Alice","extra":"retained"}""");
        }
        else if (shape == "nested")
        {
            root["type"] = "object";
            root["properties"] = new JsonObject { ["value"] = new JsonObject { [union] = alternatives } };
            input = Parse(union == "oneOf" ? """{"value":{"name":"Alice","extra":"retained"}}""" : """{"value":{"name":"Alice"}}""");
        }
        else
        {
            root["type"] = "object";
            root["properties"] = new JsonObject { ["name"] = new JsonObject { ["type"] = "string" } };
            root[union] = JsonNode.Parse("""[{"properties":{"value":{"type":"string"}}},{"properties":{"other":{"type":"integer"}}}]""");
            input = Parse("""{"name":"Alice"}""");
        }
        if (pii && shape != "whole")
        {
            if (shape == "root")
            {
                root = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["value"] = root } };
                input = new JsonObject { ["value"] = input };
                if (union == "anyOf") input["value"]!.AsObject().Remove("extra");
            }
            root["properties"]!["guard"] = new JsonObject { ["type"] = "string", ["compliance"] = Marker() };
            input["guard"] = "private";
        }
        return (root, input);
    }

    static (JsonObject Root, JsonObject Input) Restoration(string kind)
    {
        var property = kind switch
        {
            "member" => $$"""{"allOf":[{"type":"string"}],{{MarkerText}}}""",
            "reference" => """{"allOf":[{"$ref":"#/$defs/personal"}]}""",
            "composition" => $$"""{"allOf":[{"type":"string"},{ {{MarkerText}} }]}""",
            "unresolved" => """{"$ref":"#/$defs/missing"}""",
            "duplicate" => """{"allOf":[{"type":"integer"}]}""",
            _ => """{"allOf":[{"type":"string"}]}"""
        };
        var composition = kind switch
        {
            "container" or "container_undeclared" => $$""", "allOf":[{ {{MarkerText}} }]""",
            "duplicate" => $$""", "allOf":[{"properties":{"value":{"allOf":[{"type":"integer"}],{{MarkerText}} } } }]""",
            _ => string.Empty
        };
        var root = Parse($$"""{"type":"object","$defs":{"personal":{"type":"string",{{MarkerText}} } },"properties":{"value":{{property}},"guard":{"type":"string",{{MarkerText}} } } {{composition}} }""");
        var input = Parse("""{"value":"original","guard":"guard"}""");
        if (kind == "duplicate") input["value"] = 123;
        if (kind == "container_undeclared") input["extra"] = "personal-value";
        return (root, input);
    }

    static (JsonObject Root, JsonObject Input) MissingSubject(bool partialSubjects, bool alternateCasing)
    {
        var root = Parse("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"other":{"type":"string"}}}""");
        var input = Parse("""{"value":"personal-value","other":"public-value"}""");
        if (alternateCasing)
        {
            input["Value"] = input["value"]!.DeepClone();
            input.Remove("value");
        }
        if (partialSubjects) input["__subjects"] = new JsonObject { ["other"] = "subject" };
        return (root, input);
    }

    static (JsonObject Root, JsonObject Input) Append(string shape)
    {
        var member = shape switch
        {
            "reference" => """{"$ref":"#/$defs/missing"}""",
            "union" => $$"""{"anyOf":[{"type":"string"},{"type":"string",{{MarkerText}}}]}""",
            "conflict" => $$"""{"allOf":[{"type":"string"},{"type":"integer",{{MarkerText}}}]}""",
            "dynamic" => $$"""{"type":"object","additionalProperties":{"type":"string",{{MarkerText}} } }""",
            "tuple" => $$"""{"type":"array","items":[{"type":"string",{{MarkerText}} }]}""",
            "malformed" => """{"type":"string","compliance":[{"metadataType":"PII"}]}""",
            _ => """{"$ref":"#/$defs/cycle"}"""
        };
        return (Parse($$"""{"type":"object","$defs":{"cycle":{"$ref":"#/$defs/cycle"} },"properties":{"guard":{"type":"string",{{MarkerText}} },"value":{{member}} } }"""), Parse("""{"guard":"guard","value":"personal-value"}"""));
    }

    static (JsonObject Root, JsonObject Input) Cycle(string shape, bool pii)
    {
        var root = Parse("""{"$ref":"#/$defs/a","$defs":{"a":{"$ref":"#/$defs/a"},"b":{"$ref":"#/$defs/a"}}}""");
        if (shape == "mutual") root["$defs"]!["a"]!["$ref"] = "#/$defs/b";
        if (pii) root["$defs"]!["a"]!["compliance"] = Marker();
        return (root, new JsonObject());
    }

    static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
    static JsonArray Marker() => new(new JsonObject { ["metadataType"] = "PII", ["details"] = string.Empty });
}
