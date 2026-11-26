// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Json;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>Compares metadata helpers and both complete entry conversions with 81fb14c.</summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EventMetadataSerializationBenchmark
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new PointJsonConverter(), new LineStringJsonConverter(), new PolygonJsonConverter() }
    };
    static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    static readonly CorrelationId _correlation = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    static readonly EventType _eventType = new("event", 1);
    static readonly EventSequenceNumber _sequence = new(42);
    static readonly EventSourceId _source = new("source");
    static readonly EventHash _hash = new("hash");
    static readonly Subject _subject = new("subject");
    IEnumerable<Causation> _causation = null!;
    IdentityId[] _identities = null!;
    Tag[] _tags = null!;
    ExpandoObject _content = null!;
    Dictionary<EventTypeGeneration, ExpandoObject> _generations = null!;
    Dictionary<EventTypeGeneration, EventHash> _hashes = null!;

    /// <summary>Gets or sets the metadata chain length.</summary>
    [Params(0, 1, 8)]
    public int Count { get; set; }

    /// <summary>Creates deterministic chains and verifies the unchanged reference output.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _causation = [.. Enumerable.Range(0, Count).Select(index => new Causation(_occurred.AddMinutes(index), "cause", new Dictionary<string, string> { ["index"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture), ["escaped"] = "é😀<>&\"\\\n" }))];
        _identities = [.. Enumerable.Range(0, Count).Select(index => new IdentityId(new Guid(index, 0, 0, new byte[8])))];
        _tags = [.. Enumerable.Range(0, Count).Select(index => new Tag($"tag-{index}-é<>&"))];
        _content = new();
        ((IDictionary<string, object?>)_content)["name"] = "content-é";
        _generations = new() { [1] = _content };
        _hashes = new() { [1] = new("hash") };
        VerifyEquivalent(HelpersBaseline(), HelpersCurrent());
        VerifyEquivalent(JsonSerializer.Serialize(SingleBaseline()), JsonSerializer.Serialize(SingleCurrent()));
        VerifyEquivalent(JsonSerializer.Serialize(MultiBaseline()), JsonSerializer.Serialize(MultiCurrent()));
    }

    /// <summary>Serializes with the unchanged helper bundle.</summary>
    /// <returns>All three stored strings.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Helpers")]
    public (string Causation, string Identities, string Tags) HelpersBaseline() => (JsonSerializer.Serialize(_causation, _options), JsonSerializer.Serialize(_identities.Select(id => id.ToString()), _options), BaselineTags());

    /// <summary>Serializes with the production helper bundle.</summary>
    /// <returns>All three stored strings.</returns>
    [Benchmark]
    [BenchmarkCategory("Helpers")]
    public (string Causation, string Identities, string Tags) HelpersCurrent() => (EventEntryConverter.SerializeCausation(_causation), EventEntryConverter.SerializeCausedBy(_identities), EventEntryConverter.SerializeTags(_tags));

    /// <summary>Converts a single generation with the unchanged append implementation.</summary>
    /// <returns>The entire entry.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Single")]
    public EventEntry SingleBaseline() => BaselineEntry(
        new Dictionary<string, object> { { _eventType.Generation.ToString(), _content } },
        JsonSerializer.Serialize(new Dictionary<string, string> { { ((uint)_eventType.Generation).ToString(), _hash.Value } }, _options));

    /// <summary>Converts a single generation through the production append path.</summary>
    /// <returns>The entire entry.</returns>
    [Benchmark]
    [BenchmarkCategory("Single")]
    public EventEntry SingleCurrent() => EventEntryConverter.ToEventEntry(_sequence, EventSourceType.Default, _source, EventStreamType.All, EventStreamId.Default, _eventType, _correlation, _causation, _identities, _tags, _occurred, _content, _hash, _subject);

    /// <summary>Converts multiple generations with the unchanged append implementation.</summary>
    /// <returns>The entire entry.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Multi")]
    public EventEntry MultiBaseline() => BaselineEntry(
        _generations.ToDictionary(kvp => ((uint)kvp.Key).ToString(), kvp => (object)kvp.Value),
        JsonSerializer.Serialize(_hashes.ToDictionary(kvp => ((uint)kvp.Key).ToString(), kvp => kvp.Value.Value), _options));

    /// <summary>Converts multiple generations through the production append path.</summary>
    /// <returns>The entire entry.</returns>
    [Benchmark]
    [BenchmarkCategory("Multi")]
    public EventEntry MultiCurrent() => EventEntryConverter.ToEventEntry(_sequence, EventSourceType.Default, _source, EventStreamType.All, EventStreamId.Default, _eventType, _correlation, _causation, _identities, _tags, _occurred, _generations, _hashes, _subject);

    static void VerifyEquivalent<T>(T baseline, T current)
    {
        if (!EqualityComparer<T>.Default.Equals(baseline, current))
        {
            throw new InvalidOperationException("Metadata benchmark implementations disagree.");
        }
    }

    /// <summary>Uses the unchanged construction and serialization expressions from 81fb14c, shared only within the baseline.</summary>
    /// <param name="content">The generation-keyed event content.</param>
    /// <param name="hashes">The serialized generation-keyed hashes.</param>
    /// <returns>The complete stored event entry.</returns>
    EventEntry BaselineEntry(Dictionary<string, object> content, string hashes) => new()
    {
        SequenceNumber = _sequence.Value,
        CorrelationId = _correlation.ToString(),
        Causation = JsonSerializer.Serialize(_causation, _options),
        CausedBy = JsonSerializer.Serialize(_identities.Select(id => id.ToString()), _options),
        Type = _eventType.Id,
        AppendedGeneration = _eventType.Generation.Value,
        Occurred = _occurred,
        EventSourceType = EventSourceType.Default,
        EventSourceId = _source,
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        Content = JsonSerializer.Serialize(content, _options),
        ContentHashes = hashes,
        Compensations = new Dictionary<string, string>(),
        Subject = _subject.Value,
        Tags = BaselineTags()
    };

    string BaselineTags()
    {
        var values = _tags.Select(tag => tag.Value).ToArray();
        return values.Length == 0 ? string.Empty : JsonSerializer.Serialize(values, _options);
    }
}
