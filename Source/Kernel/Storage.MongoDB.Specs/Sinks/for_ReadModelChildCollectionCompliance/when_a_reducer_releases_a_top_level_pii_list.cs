// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Regression for the reducer RELEASE of a coarse <c>[PII]</c> list at the top level of a reducer-owned read
/// model. The whole list is blob-encrypted to a single ciphertext string at rest; releasing it must restore
/// the array so it deserializes into the read model's <c>IReadOnlyList&lt;T&gt;</c> — the exact step the
/// client reducer performs on its next reduce. Before the fix the released value stayed a string and that
/// deserialize threw, freezing the reducer. Two reduces are driven against one key: create, then
/// read-modify-write.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_reducer_releases_a_top_level_pii_list(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    const string Criterion = "quality";
    const int Score = 5;

    static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    Exception? _error;
    bool _storedScoresAreCiphertextString;
    bool _releasedScoresAreArray;
    IReadOnlyList<CriterionScore>? _deserialized;

    protected override string Identifier => "req-1";

    protected override ReadModelObserverType ObserverType => ReadModelObserverType.Reducer;

    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "scores": {
              "type": "array",
              "compliance": [ { "metadataType": "PII", "details": "" } ],
              "items": {
                "type": "object",
                "properties": {
                  "criterion": { "type": "string" },
                  "score": { "type": "integer" }
                }
              }
            }
          }
        }
        """;

    async Task Because()
    {
        var pipeline = new ReducerPipeline(ReadModel, Sink, ObjectComparer, Compliance, EventStore, EventStoreNamespace);

        // First reduce creates the read model with a coarse [PII] scores list.
        await pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), State())));

        // Second reduce reads the stored (encrypted) state and releases it before reducing again.
        _error = await Catch.Exception(() => pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), State()))));

        var storedScores = (await StoredDocument())["scores"];
        _storedScoresAreCiphertextString = storedScores.IsString && IsBase64(storedScores.AsString);

        if (storedScores.IsString)
        {
            var released = await ComplianceManager.Release(
                EventStore,
                EventStoreNamespace,
                Schema,
                Identifier,
                new JsonObject { ["id"] = "req-1", ["scores"] = storedScores.AsString });
            _releasedScoresAreArray = released["scores"] is JsonArray;

            // Mirror the client reducer's state load: deserialize the released list into its CLR type.
            _deserialized = released["scores"].Deserialize<IReadOnlyList<CriterionScore>>(_jsonOptions);
        }
    }

    [Fact] void should_release_the_stored_state_without_failing() => _error.ShouldBeNull();

    [Fact] void should_store_the_pii_list_as_ciphertext_string() => _storedScoresAreCiphertextString.ShouldBeTrue();

    [Fact] void should_release_the_pii_list_back_into_an_array() => _releasedScoresAreArray.ShouldBeTrue();

    [Fact] void should_deserialize_into_the_read_model_list_type() => _deserialized!.Count.ShouldEqual(1);

    [Fact] void should_round_trip_the_element_value() => _deserialized![0].Criterion.ShouldEqual(Criterion);

    AppendedEvent Event() =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject State()
    {
        var score = Expando(("criterion", Criterion), ("score", Score));
        return Expando(("id", "req-1"), ("scores", new object[] { score }));
    }

    record CriterionScore(string Criterion, int Score);
}
