// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark.given;

/// <summary>
/// The arrangement every watermark case shares: one read model accumulating a count, written through
/// whichever sink the harness supplies.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class an_accumulating_read_model<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    protected const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "count": { "type": "integer" }
          }
        }
        """;

    protected ISink _sink;
    protected Key _key;

    THarness _harness;

    void Establish()
    {
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);
        _harness = new THarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
    }

    void Destroy() => _harness.Dispose();

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingCountTo(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;

        PropertyDifference[] differences = [new PropertyDifference(new PropertyPath("count"), null, count)];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [propertiesChanged];
        changeset.Changes.Returns(changes);
        return changeset;
    }

    /// <summary>
    /// Reads the accumulated count back through the sink.
    /// </summary>
    /// <returns>The count currently stored for the read model.</returns>
    /// <remarks>
    /// Converted rather than cast because the backends round-trip an integer through different CLR
    /// types - SQLite hands back a long where the in-memory sink keeps the int it was given.
    /// </remarks>
    protected async Task<int> CurrentCount()
    {
        var instance = await _sink.FindOrDefault(_key);
        return Convert.ToInt32(((IDictionary<string, object?>)instance!)["count"], CultureInfo.InvariantCulture);
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            ContainerName,
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromJson(SchemaJson) }
            },
            []);
}
