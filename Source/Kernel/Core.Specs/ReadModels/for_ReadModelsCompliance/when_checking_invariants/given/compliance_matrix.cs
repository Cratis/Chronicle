// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class compliance_matrix
{
    public static readonly string[] Shapes = ["flat", "own_and_referenced_base", "inline_allOf", "multiple_inline_groups", "reference_siblings", "nested_object", "object_array", "scalar_array", "recursive", "undeclared_key", "undeclared_pascal_key", "declared_key", "case_distinct", "referenced_member", "composed_member", "duplicate_member", "reference_chain", "referenced_scalar_array", "anchored_member", "anchored_anyOf", "anchored_oneOf"];
    public static readonly string[] Members = ["string", "empty_string", "nullable_string", "nullable_empty_string", "nullable_null", "integer", "boolean", "guid", "date", "decimal", "referenced_enum", "value_object", "nullable_value_object", "null_value_object", "null"];
    public static readonly string[] Protections = ["pii", "non_pii", "undeclared"];

    public static TheoryData<string, bool, string, string, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, bool, string, string, bool>();
            var combinations = from shape in Shapes
                               from erased in new[] { false, true }
                               from member in Members
                               from protection in Protections
                               from pipeline in new[] { false, true }
                               where shape is not "anchored_anyOf" and not "anchored_oneOf" || protection == "pii"
                               select (shape, erased, member, protection, pipeline);
            foreach (var (shape, erased, member, protection, pipeline) in combinations) cells.Add(shape, erased, member, protection, pipeline);
            return cells;
        }
    }

    public static specimen Create(string shape, string member, string protection, bool includeGuard = true)
    {
        var leaf = MemberSchema(member);
        if (protection == "pii") leaf["compliance"] = Marker();
        var root = ObjectSchema();
        root["$defs"] = new JsonObject
        {
            ["kind"] = JsonNode.Parse("""{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Known"]}""")
        };
        var paths = new List<string>();
        var values = new List<(string Name, object? Value)>();
        var value = MemberValue(member);
        var local = ObjectSchema();
        AddMember(local, "value", leaf, protection);
        var key = shape == "undeclared_pascal_key" ? "Id" : "id";
        if (shape == "undeclared_pascal_key")
        {
            local["properties"]!["Value"] = local["properties"]!["value"]?.DeepClone();
            local["properties"]!.AsObject().Remove("value");
        }
        switch (shape)
        {
            case "own_and_referenced_base":
            case "inline_allOf":
            case "reference_siblings":
                var baseSchema = ObjectSchema();
                AddMember(baseSchema, "inherited", leaf, protection);
                root["$defs"]!["base"] = baseSchema;
                root["properties"] = local["properties"]!.DeepClone();
                if (shape == "reference_siblings")
                {
                    root["$ref"] = "#/$defs/base";
                }
                else
                {
                    root["allOf"] = shape == "inline_allOf"
                        ? new JsonArray(new JsonObject { ["$ref"] = "#/$defs/base" }, local.DeepClone())
                        : new JsonArray(new JsonObject { ["$ref"] = "#/$defs/base" });
                }
                values.Add(("inherited", value));
                paths.Add("inherited");
                values.Add(("value", value));
                paths.Add("value");
                break;
            case "multiple_inline_groups":
                var second = ObjectSchema();
                AddMember(second, "other", leaf, protection);
                root["allOf"] = new JsonArray(local.DeepClone(), second);
                values.Add(("value", value));
                values.Add(("other", value));
                paths.AddRange(["value", "other"]);
                break;
            case "nested_object":
                root["properties"]!["nested"] = local;
                values.Add(("nested", State(("value", value))));
                paths.Add("nested.value");
                break;
            case "object_array":
                root["properties"]!["items"] = new JsonObject { ["type"] = "array", ["items"] = local };
                values.Add(("items", new object[] { State(("value", value)), State(("value", value)) }));
                paths.AddRange(["items.0.value", "items.1.value"]);
                break;
            case "scalar_array":
                AddMember(root, "items", new JsonObject { ["type"] = "array", ["items"] = leaf }, protection);
                values.Add(("items", new object?[] { value, value, null }));
                paths.AddRange(["items.0", "items.1"]);
                break;
            case "recursive":
                local["properties"]!["next"] = new JsonObject { ["$ref"] = "#/$defs/link" };
                root["$defs"]!["link"] = local;
                root["properties"]!["link"] = new JsonObject { ["$ref"] = "#/$defs/link" };
                values.Add(("link", State(("value", value), ("next", State(("value", value))))));
                paths.AddRange(["link.value", "link.next.value"]);
                break;
            case "case_distinct":
                root["properties"] = local["properties"]!.DeepClone();
                root["properties"]!["Value"] = MemberSchema(member);
                values.Add(("value", value));
                values.Add(("Value", value));
                paths.Add("value");
                break;
            case "referenced_scalar_array":
                root["$defs"]!["member"] = leaf;
                root["$defs"]!["alias"] = new JsonObject { ["$ref"] = "#/$defs/member" };
                AddMember(root, "items", new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["$ref"] = "#/$defs/alias" } }, protection);
                values.Add(("items", new object?[] { value, value, null }));
                paths.AddRange(["items.0", "items.1"]);
                break;
            case "anchored_member":
            case "anchored_anyOf":
            case "anchored_oneOf":
                leaf["$anchor"] = "personal";
                root["$defs"]!["member"] = leaf;
                var anchored = new JsonObject { ["$ref"] = "#personal" };
                var anchoredMember = shape == "anchored_member" ? anchored : new JsonObject
                {
                    [shape == "anchored_anyOf" ? "anyOf" : "oneOf"] = new JsonArray(anchored)
                };
                AddMember(root, "value", anchoredMember, protection);
                values.Add(("value", value));
                paths.Add("value");
                break;
            case "reference_chain":
            case "referenced_member":
            case "composed_member":
                root["$defs"]!["member"] = leaf;
                root["$defs"]!["alias"] = new JsonObject { ["$ref"] = "#/$defs/member" };
                var reference = new JsonObject { ["$ref"] = shape == "reference_chain" ? "#/$defs/alias" : "#/$defs/member" };
                AddMember(root, "value", shape != "composed_member" ? reference : new JsonObject { ["allOf"] = new JsonArray(reference) }, protection);
                values.Add(("value", value));
                paths.Add("value");
                break;
            case "duplicate_member":
                root["properties"]!["value"] = MemberSchema(member);
                root["allOf"] = new JsonArray(local);
                values.Add(("value", value));
                paths.Add("value");
                break;
            default:
                root["properties"] = local["properties"]!.DeepClone();
                var name = shape == "undeclared_pascal_key" ? "Value" : "value";
                values.Add((name, value));
                paths.Add(name);
                break;
        }
        var guard = shape == "undeclared_pascal_key" ? "Guard" : "guard";
        if (includeGuard)
        {
            root["properties"]![guard] = new JsonObject { ["type"] = "string", ["compliance"] = Marker() };
            values.Add((guard, "guard-secret"));
        }
        if (shape is not "undeclared_key" and not "undeclared_pascal_key") root["properties"]![key] = new JsonObject { ["type"] = "string" };
        values.Add((key, "matrix-subject"));
        values.Add(("__initialized", true));
        values.Add(("__lastHandledEventSequenceNumber", 42L));
        values.Add(("extra", "undeclared-state"));
        return new(JsonSchema.FromJson(root.ToJsonString()), State([.. values]), paths, key, guard);
    }

    public static object? MemberValue(string member) => member switch
    {
        "string" or "nullable_string" => "personal-value",
        "empty_string" or "nullable_empty_string" => string.Empty,
        "nullable_null" or "null_value_object" or "null" => null,
        "integer" => 123,
        "boolean" => true,
        "guid" => Guid.Parse("756848d4-fbe1-4bb9-ad3b-c11c4aa0c9b6"),
        "date" => new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        "decimal" => 12345.67890123456789m,
        "referenced_enum" => kind.Known,
        "value_object" or "nullable_value_object" => State(("count", 123), ("flag", true), ("identifier", MemberValue("guid")), ("date", MemberValue("date"))),
        _ => throw new ArgumentOutOfRangeException(nameof(member))
    };

    public static object? ErasedValue(string member) => member switch
    {
        "string" or "empty_string" => string.Empty,
        "nullable_string" or "nullable_empty_string" or "nullable_null" or "null_value_object" or "null" => null,
        "integer" or "referenced_enum" => 0,
        "boolean" => false,
        "guid" => Guid.Empty,
        "date" => default(DateTime),
        "decimal" => 0m,
        "value_object" or "nullable_value_object" => State(("count", 0), ("flag", false), ("identifier", Guid.Empty), ("date", default(DateTime))),
        _ => throw new ArgumentOutOfRangeException(nameof(member))
    };

    public static ExpandoObject State(params (string Name, object? Value)[] values)
    {
        var state = new ExpandoObject();
        foreach (var (name, value) in values) ((IDictionary<string, object?>)state)[name] = value;
        return state;
    }

    public static JsonNode? At(object value, string path)
    {
        var node = JsonSerializer.SerializeToNode(value);
        foreach (var segment in path.Split('.'))
        {
            node = node is JsonArray array ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)] : (node as JsonObject)?[segment];
        }
        return node;
    }

    static JsonObject ObjectSchema() => new() { ["type"] = "object", ["properties"] = new JsonObject() };
    static JsonArray Marker() => new(new JsonObject { ["metadataType"] = "PII", ["details"] = string.Empty });
    static void AddMember(JsonObject schema, string name, JsonObject member, string protection)
    {
        if (protection != "undeclared") schema["properties"]![name] = member.DeepClone();
    }

    static JsonObject MemberSchema(string member) => JsonNode.Parse(member switch
    {
        "string" or "empty_string" => """{"type":"string"}""",
        "nullable_string" or "nullable_empty_string" or "nullable_null" => """{"type":["string","null"]}""",
        "integer" => """{"type":"integer","format":"int32"}""",
        "boolean" => """{"type":"boolean"}""",
        "guid" => """{"type":"string","format":"guid"}""",
        "date" => """{"type":"string","format":"date-time"}""",
        "decimal" => """{"type":"number","format":"decimal"}""",
        "referenced_enum" => """{"$ref":"#/$defs/kind"}""",
        "value_object" => """{"type":"object","properties":{"count":{"type":"integer","format":"int32"},"flag":{"type":"boolean"},"identifier":{"type":"string","format":"guid"},"date":{"type":"string","format":"date-time"}}}""",
        "nullable_value_object" or "null_value_object" => """{"anyOf":[{"type":"object","properties":{"count":{"type":"integer","format":"int32"},"flag":{"type":"boolean"},"identifier":{"type":"string","format":"guid"},"date":{"type":"string","format":"date-time"}}},{"type":"null"}]}""",
        "null" => """{"type":["integer","null"]}""",
        _ => throw new ArgumentOutOfRangeException(nameof(member))
    })!.AsObject();

    public record specimen(JsonSchema Schema, ExpandoObject State, IReadOnlyList<string> Paths, string Key, string Guard);
    enum kind
    {
        Unknown = 0,
        Known = 1
    }
}
