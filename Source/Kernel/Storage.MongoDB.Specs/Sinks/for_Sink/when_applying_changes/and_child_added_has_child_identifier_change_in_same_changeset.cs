// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Driver;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_child_added_has_child_identifier_change_in_same_changeset.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_child_added_has_child_identifier_change_in_same_changeset(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        const string ContractId = "contract-1";
        const string CandidateId = "candidate-1";

        IMongoClient _client = default!;
        IMongoDatabase _database = default!;
        Sink _sink = default!;
        Key _key = default!;
        string _databaseName = default!;

        public Exception? Error;
        public ExpandoObject? Result;

        public async Task InitializeAsync()
        {
            _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            _database = _client.GetDatabase(_databaseName);
            _key = new Key(ContractId, ArrayIndexers.NoIndexers);

            var readModel = CreateReadModelDefinition();
            var typeFormats = new TypeFormats();
            var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
            var collections = new SinkCollections(readModel, _database);
            var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
            _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);

            try
            {
                await _sink.ApplyChanges(_key, CreateChangeset(), EventSequenceNumber.First);
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

        public ExpandoObject[] GetCandidates()
        {
            var result = (IDictionary<string, object?>)Result!;
            return ((IEnumerable<object>)result["candidates"]!).Cast<ExpandoObject>().ToArray();
        }

        public IDictionary<string, object?> GetCandidate() => GetCandidates()[0];

        static IChangeset<AppendedEvent, ExpandoObject> CreateChangeset()
        {
            dynamic child = new ExpandoObject();
            child.candidateId = CandidateId;
            child.name = "Ada Lovelace";

            var candidatesProperty = new PropertyPath("candidates");
            var childAdded = new ChildAdded(child, candidatesProperty, new PropertyPath("candidateId"), CandidateId, ArrayIndexers.NoIndexers);
            var arrayIndexers = new ArrayIndexers(
            [
                new ArrayIndexer(candidatesProperty, new PropertyPath("candidateId"), CandidateId)
            ]);
            var identifierChanged = new PropertyDifference("[candidates].candidateId", null, CandidateId, arrayIndexers);
            var propertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [identifierChanged]);

            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.Changes.Returns([childAdded, propertiesChanged]);
            changeset.HasBeenRemoved().Returns(false);
            changeset.HasJoined().Returns(false);

            return changeset;
        }

        static ReadModelDefinition CreateReadModelDefinition() =>
            new(
                "test-contract-read-model",
                "TestContractReadModel",
                $"contracts_{Guid.NewGuid():N}",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema>
                {
                    { ReadModelGeneration.First, JsonSchema.FromType<Contract>() }
                },
                []);

        record Candidate(string CandidateId, string Name);
        record Contract(string Id, IEnumerable<Candidate> Candidates);
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_find_the_contract() => ctx.Result.ShouldNotBeNull();
    [Fact] void should_have_one_candidate() => ctx.GetCandidates().Length.ShouldEqual(1);
    [Fact] void should_keep_candidate_identifier_on_the_pushed_child() => ctx.GetCandidate()["candidateId"].ShouldEqual("candidate-1");
    [Fact] void should_keep_candidate_name_on_the_pushed_child() => ctx.GetCandidate()["name"].ShouldEqual("Ada Lovelace");
}
