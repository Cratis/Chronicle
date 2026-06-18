// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_reducer_changes_a_field_on_an_existing_child.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_reducer_changes_a_field_on_an_existing_child(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        const string TeamId = "team-1";
        const string MemberId = "member-1";

        readonly ObjectComparer _objectComparer = new();

        IMongoClient _client = default!;
        IMongoDatabase _database = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        MongoDBConverter _mongoDBConverter = default!;
        ExpandoObjectConverter _expandoObjectConverter = default!;
        Sink _sink = default!;
        ReducerPipeline _pipeline = default!;
        JsonSchema _schema = default!;
        Key _key = default!;
        string _databaseName = default!;

        public Exception? Error;
        public ExpandoObject? Result;
        public PropertyDifference[] RawDifferences = [];

        public async Task InitializeAsync()
        {
            _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            _database = _client.GetDatabase(_databaseName);
            _schema = JsonSchema.FromType<Team>();
            _key = new Key(TeamId, ArrayIndexers.NoIndexers);

            var readModel = CreateReadModelDefinition();
            var typeFormats = new TypeFormats();
            _expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
            var collections = new SinkCollections(readModel, _database);
            _mongoDBConverter = new MongoDBConverter(_expandoObjectConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, _mongoDBConverter, collections, _expandoObjectConverter);
            _sink = new Sink(readModel, _mongoDBConverter, collections, changesetConverter, _expandoObjectConverter);
            _pipeline = new ReducerPipeline(readModel, _sink, _objectComparer, new PassthroughReadModelsCompliance(), "test-store", "test-namespace");
            _collection = collections.GetCollection();

            var initial = CreateTeam("pending");
            await InsertInitialDocument(initial);

            var changed = CreateTeam("approved");
            _objectComparer.Compare(initial, changed, out var rawDifferences);
            RawDifferences = rawDifferences.ToArray();

            try
            {
                await _pipeline.Handle(
                    new ReducerContext([CreateEvent()], _key),
                    (_, _) => Task.FromResult(new ReducerSubscriberResult(
                        ObserverSubscriberResult.Ok(EventSequenceNumber.First),
                        changed)));
            }
            catch (Exception error)
            {
                Error = error;
            }

            Result = await _sink.FindOrDefault(_key);
        }

        public async Task DisposeAsync()
        {
            if (_databaseName is not null)
            {
                await _client.DropDatabaseAsync(_databaseName);
            }
        }

        public ExpandoObject[] GetMembers()
        {
            var result = (IDictionary<string, object?>)Result!;
            return ((IEnumerable<object>)result["members"]!).Cast<ExpandoObject>().ToArray();
        }

        public string GetMemberStatus()
        {
            var member = (IDictionary<string, object?>)GetMembers()[0];
            return (string)member["status"]!;
        }

        async Task InsertInitialDocument(ExpandoObject initial)
        {
            var document = _expandoObjectConverter.ToBsonDocument(initial, _schema);
            document["_id"] = _mongoDBConverter.ToBsonValue(_key);
            await _collection.InsertOneAsync(document);
        }

        static ExpandoObject CreateTeam(string memberStatus)
        {
            dynamic member = new ExpandoObject();
            member.memberId = MemberId;
            member.status = memberStatus;

            dynamic team = new ExpandoObject();
            team.id = TeamId;
            team.members = new object[] { member };

            return team;
        }

        ReadModelDefinition CreateReadModelDefinition() =>
            new(
                "test-team-read-model",
                "TestTeamReadModel",
                $"teams_{Guid.NewGuid():N}",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Reducer,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema>
                {
                    { ReadModelGeneration.First, _schema }
                },
                []);

        static AppendedEvent CreateEvent()
        {
            var context = EventContext.From(
                "test-store",
                "test-namespace",
                EventType.Unknown,
                EventSourceType.Default,
                TeamId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet);

            return new AppendedEvent(context, new ExpandoObject());
        }

        record Member(string MemberId, string Status);
        record Team(string Id, IEnumerable<Member> Members);

        sealed class PassthroughReadModelsCompliance : IReadModelsCompliance
        {
            public Task<ExpandoObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, ExpandoObject instance) =>
                Task.FromResult(instance);

            public Task<JsonObject> ReleaseJson(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, JsonObject instance) =>
                Task.FromResult(instance);

            public Task<ExpandoObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, ExpandoObject instance) =>
                Task.FromResult(instance);

            public Task<IEnumerable<ExpandoObject>> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, IEnumerable<ExpandoObject> instances) =>
                Task.FromResult(instances);
        }
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_keep_object_comparer_fine_grained() => ctx.RawDifferences[0].PropertyPath.Path.ShouldEqual("[members].status");
    [Fact] void should_find_the_team() => ctx.Result.ShouldNotBeNull();
    [Fact] void should_keep_one_member() => ctx.GetMembers().Length.ShouldEqual(1);
    [Fact] void should_update_the_member_status() => ctx.GetMemberStatus().ShouldEqual("approved");
}
