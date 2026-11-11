// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Measures the production BSON conversion paths; delegates bind once so this fixture also runs on main.
/// </summary>
[MemoryDiagnoser]
public class EventContentBsonBenchmark
{
    Func<string, JsonSchema?, BsonDocument> _fromJson = null!;
    Func<BsonDocument, string> _toJson = null!;
    string _content = null!;
    JsonSchema? _schema;
    BsonDocument _document = null!;

    /// <summary>
    /// Gets or sets the schema path to exercise, including inherited decimal properties.
    /// </summary>
    [Params("NoSchema", "Plain", "Decimal", "Inherited")]
    public string Schema { get; set; } = "NoSchema";

    /// <summary>
    /// Gets or sets the number of nested numeric objects in the payload.
    /// </summary>
    [Params(8, 128)]
    public int Count { get; set; }

    /// <summary>
    /// Binds production methods and creates reused payloads and immutable schemas outside measurement.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var type = typeof(ClusterStorage).Assembly.GetType("Cratis.Chronicle.Storage.MongoDB.EventContentBson", throwOnError: true)!;
        _fromJson = type.GetMethod("FromJson", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<string, JsonSchema?, BsonDocument>>();
        _toJson = type.GetMethod("ToJson", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<BsonDocument, string>>();
        _schema = Schema switch
        {
            "NoSchema" => null,
            "Plain" => JsonSchema.FromJson("""{"type":"object","properties":{"values":{"type":"array","items":{"type":"object","properties":{"amount":{"type":"number"},"count":{"type":"integer"}}}}}}"""),
            "Decimal" => JsonSchema.FromJson("""{"type":"object","properties":{"values":{"type":"array","items":{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"count":{"type":"integer"}}}}}}"""),
            _ => JsonSchema.FromJson("""{"type":"object","properties":{"values":{"type":"array","items":{"allOf":[{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"count":{"type":"integer"}}}]}}}}""")
        };
        _content = JsonSerializer.Serialize(new { values = Enumerable.Range(0, Count).Select(index => new { amount = 0.1234567890123456789012345678m, count = index }).ToArray() });
        _document = _fromJson(_content, _schema);
    }

    /// <summary>
    /// Converts numeric JSON through the production scanner and schema preparation.
    /// </summary>
    /// <returns>The converted BSON.</returns>
    [Benchmark]
    public BsonDocument FromJson() => _fromJson(_content, _schema);

    /// <summary>
    /// Converts BSON through the production decimal detection and restoration.
    /// </summary>
    /// <returns>The restored JSON.</returns>
    [Benchmark]
    public string ToJson() => _toJson(_document);
}
