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
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark.given;

/// <summary>
/// Base for the watermark guard scenarios against a real MongoDB collection. The guard is expressed as an extra
/// clause on the update filter, so only the server can prove it.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class an_accumulating_read_model(MongoDBFixture fixture) : Specification
{
    IMongoClient _client = default!;
    string _databaseName = default!;

    protected Sink _sink = default!;
    protected Key _key = default!;

    void Establish()
    {
        _databaseName = $"chronicle_watermark_specs_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);

        var readModel = CreateReadModelDefinition();
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(readModel, database);
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
        _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingCountTo(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;

        PropertyDifference[] differences = [new PropertyDifference(new PropertyPath("count"), null, count)];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.CurrentState.Returns(state);
        Change[] changes = [propertiesChanged];
        changeset.Changes.Returns(changes);
        changeset.HasBeenRemoved().Returns(false);
        changeset.HasJoined().Returns(false);
        return changeset;
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> RemovalChangeset()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.CurrentState.Returns(new ExpandoObject());
        changeset.Changes.Returns([]);
        changeset.HasBeenRemoved().Returns(true);
        changeset.HasJoined().Returns(false);
        return changeset;
    }

    protected async Task<int> CurrentCount()
    {
        var instance = await _sink.FindOrDefault(_key);
        return Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-counter-read-model",
            "TestCounterReadModel",
            $"counters_{Guid.NewGuid():N}",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromType<Counter>() }
            },
            []);

    record Counter(string Id, int Count);
}
