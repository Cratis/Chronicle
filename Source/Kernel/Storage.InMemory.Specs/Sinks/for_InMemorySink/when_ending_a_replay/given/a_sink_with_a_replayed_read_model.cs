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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_ending_a_replay.given;

public class a_sink_with_a_replayed_read_model : Specification
{
    protected InMemorySink _sink;
    protected Key _key;

    void Establish()
    {
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());
    }

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

    protected static ReplayContext ReplayContext() => new(
        new ReadModelType("test-read-model", ReadModelGeneration.First),
        "TestReadModel",
        "TestReadModel-revert",
        DateTimeOffset.UtcNow);

    protected async Task<int?> CurrentCount()
    {
        var instance = await _sink.FindOrDefault(_key);
        if (instance is null)
        {
            return null;
        }

        return Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
