// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_an_application_is_removed_after_its_project.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_an_application_is_removed_after_its_project(context ctx, ITestOutputHelper output) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        readonly string _databaseName = MongoDBSpecDatabaseNames.New();
        readonly ConcurrentQueue<BsonDocument> _commands = new();
        IMongoClient _client = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;

        public async ValueTask InitializeAsync()
        {
            var settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.DatabaseNamespace.DatabaseName == _databaseName && (command.CommandName == "update" || command.CommandName == "delete"))
                {
                    _commands.Enqueue(command.Command.DeepClone().AsBsonDocument);
                }
            });
            _client = new MongoClient(settings);
            var schema = await JsonSchema.FromJsonAsync("""
                {"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"applications":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"domains":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}}}}}}
                """);
            var readModel = new ReadModelDefinition("project-hierarchy", "ProjectHierarchy", "projectHierarchies", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
            _collection = collections.GetCollection();
        }

        public async ValueTask DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        public async Task Verify(string state, bool bulk, ITestOutputHelper output)
        {
            var projectId = $"{state}-{bulk}";
            var rootKey = new Key(projectId, ArrayIndexers.NoIndexers);
            var applicationKey = new Key(projectId, new ArrayIndexers([new ArrayIndexer("[applications]", "id", "application")]));
            dynamic application = new ExpandoObject();
            application.id = "application";
            application.name = "Shop";
            dynamic domain = new ExpandoObject();
            domain.id = "domain";
            application.domains = new List<ExpandoObject> { domain };
            dynamic initialState = new ExpandoObject();
            initialState.applications = new List<ExpandoObject> { application };
            var domainKey = new Key(projectId, new ArrayIndexers([new ArrayIndexer("[applications]", "id", "application"), new ArrayIndexer("[applications].[domains]", "id", "domain")]));
            var failures = new List<FailedPartition>();
            _commands.Clear();

            if (bulk)
            {
                await _sink.BeginBulk();
            }

            if (state == "removed" || state == "present" || state == "nested-present")
            {
                failures.AddRange(await _sink.ApplyChanges(rootKey, Changes(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "Project", ArrayIndexers.NoIndexers)])), 1UL));
                failures.AddRange(await _sink.ApplyChanges(applicationKey, Changes(new ChildAdded(application, "[applications]", "id", "application", ArrayIndexers.NoIndexers)), 2UL));
                if (state == "removed")
                {
                    var removal = Changes();
                    removal.HasBeenRemoved().Returns(true);
                    failures.AddRange(await _sink.ApplyChanges(rootKey, removal, 3UL));
                }
            }
            else if (state == "without-array")
            {
                await _collection.InsertOneAsync(new BsonDocument { { "_id", projectId }, { "name", "Project" } });
            }

            try
            {
                if (state != "nested-present")
                {
                    failures.AddRange(await _sink.ApplyChanges(applicationKey, Removal(applicationKey, "[applications]", initialState), 4UL));
                }

                // RemovedWith on nested ChildrenFrom uses the same engine operation, but its $pull
                // is positioned inside applications. The parent may already have been removed.
                failures.AddRange(await _sink.ApplyChanges(domainKey, Removal(domainKey, "[applications].[domains]", initialState), 5UL));
                if (bulk)
                {
                    failures.AddRange(await _sink.EndBulk());
                }
            }
            finally
            {
                foreach (var command in _commands)
                {
                    output.WriteLine(command.ToJson());
                }
            }

            failures.ShouldBeEmpty();
            var result = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", projectId)).SingleOrDefaultAsync();
            if (state == "removed" || state == "missing")
            {
                result.ShouldBeNull();
            }
            else
            {
                result.ShouldNotBeNull();
                result["name"].AsString.ShouldEqual("Project");
                if (state == "without-array")
                {
                    result.Contains("applications").ShouldBeFalse();
                }
                else if (state == "nested-present")
                {
                    var remainingApplication = result["applications"].AsBsonArray.Single().AsBsonDocument;
                    remainingApplication["name"].AsString.ShouldEqual("Shop");
                    remainingApplication["domains"].AsBsonArray.ShouldBeEmpty();
                }
                else
                {
                    result["applications"].AsBsonArray.ShouldBeEmpty();
                }
            }
        }

        static IChangeset<AppendedEvent, ExpandoObject> Removal(Key key, PropertyPath children, ExpandoObject initialState)
        {
            // ProjectionFactory.SetupRemovedWith wires this operation at each ChildrenFrom level.
            // Use the pre-removal snapshot so a late nested event still produces a child removal.
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, initialState);
            using var events = new Subject<ProjectionEventContext>();
            using var subscription = events.RemoveChild(children, "id").Subscribe();
            events.OnNext(new(key, null!, changeset, ProjectionOperationType.Remove, false));
            changeset.Changes.OfType<ChildRemoved>().Count().ShouldEqual(1);
            return changeset;
        }

        static IChangeset<AppendedEvent, ExpandoObject> Changes(params Change[] changes)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(new ExpandoObject());
            changeset.Changes.Returns(changes);
            return changeset;
        }
    }

    [Theory]
    [InlineData("removed", false)]
    [InlineData("removed", true)]
    [InlineData("without-array", false)]
    [InlineData("without-array", true)]
    [InlineData("missing", false)]
    [InlineData("missing", true)]
    [InlineData("present", false)]
    [InlineData("present", true)]
    [InlineData("nested-present", false)]
    [InlineData("nested-present", true)]
    public Task should_remove_only_the_application_without_resurrecting_the_project(string state, bool bulk) => ctx.Verify(state, bulk, output);
}
