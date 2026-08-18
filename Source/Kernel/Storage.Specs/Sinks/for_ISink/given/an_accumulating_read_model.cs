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
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.given;

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
        _harness = CreateHarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The <typeparamref name="THarness"/> to run the contract through.</returns>
    /// <remarks>
    /// Overridable because a backend needing infrastructure receives it through the constructor - a
    /// container fixture, say - and so cannot be built by the contract itself.
    /// </remarks>
    protected virtual THarness CreateHarness() => new();

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
    protected async Task<int> CurrentCount() => (await CurrentCountOrNull())!.Value;

    /// <summary>
    /// Reads the accumulated count back, or null when the read model is not there at all.
    /// </summary>
    /// <returns>The count currently stored for the read model, or null when it does not exist.</returns>
    protected async Task<int?> CurrentCountOrNull()
    {
        var instance = await _sink.FindOrDefault(_key);
        if (instance is null)
        {
            return null;
        }

        return Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Builds the context a replay of this read model runs under.
    /// </summary>
    /// <returns>The <see cref="ReplayContext"/> to begin and end a replay with.</returns>
    protected static ReplayContext ReplayContext() => new(
        new ReadModelType("test-read-model", ReadModelGeneration.First),
        ContainerName,
        $"{ContainerName}-revert",
        DateTimeOffset.UtcNow);

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            ContainerName,
            "Test read model",
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
