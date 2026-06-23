// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Regression for the reducer RELEASE leg of a coarse <c>[PII]</c> list nested inside a reducer-owned
/// child collection. A coarse <c>[PII]</c> on a whole list is blob-encrypted to a single ciphertext
/// string even though its schema type stays array; the converters must round-trip it as that scalar
/// string rather than shredding it into per-character elements, otherwise the next reduce's initial
/// RELEASE fails to base64-decode it. Two reduces are driven against one key — create, then
/// read-modify-write — and the second must release the stored state cleanly with the list round-tripping.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_reducer_releases_a_child_collection_with_a_pii_list(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    const string Criterion = "quality";
    const int Score = 5;

    Exception? _error;
    bool _storedScoresAreCiphertextString;
    bool _storedScoresAreArray;
    string _releasedScores = string.Empty;

    protected override string Identifier => "req-1";

    protected override ReadModelObserverType ObserverType => ReadModelObserverType.Reducer;

    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "candidates": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "candidateId": { "type": "string" },
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                  "evaluationScores": {
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
            }
          }
        }
        """;

    async Task Because()
    {
        var pipeline = new ReducerPipeline(ReadModel, Sink, ObjectComparer, Compliance, EventStore, EventStoreNamespace);

        // First reduce creates the candidate child with a coarse [PII] evaluation-scores list.
        await pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), WorkSurface("Alice"))));

        // Second reduce reads the stored (encrypted) state and releases it before reducing again.
        _error = await Catch.Exception(() => pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), WorkSurface("Alice Smith")))));

        var storedScores = (await StoredDocument())["candidates"].AsBsonArray[0].AsBsonDocument["evaluationScores"];
        _storedScoresAreCiphertextString = storedScores.IsString && IsBase64(storedScores.AsString);
        _storedScoresAreArray = storedScores.IsBsonArray;

        if (storedScores.IsString)
        {
            // The stored ciphertext must decrypt back to the original list content, mirroring how the
            // read path releases a coarse [PII] blob to its plaintext (JSON) representation.
            var released = await ComplianceManager.Release(
                EventStore,
                EventStoreNamespace,
                Schema.GetSchemaForPropertyPath("candidates"),
                Identifier,
                new JsonObject { ["candidateId"] = "cand-1", ["evaluationScores"] = storedScores.AsString });
            _releasedScores = released["evaluationScores"]!.ToString();
        }
    }

    [Fact] void should_release_the_stored_state_without_failing() => _error.ShouldBeNull();

    [Fact] void should_store_the_pii_list_as_ciphertext_string() => _storedScoresAreCiphertextString.ShouldBeTrue();

    [Fact] void should_not_shred_the_pii_list_into_an_array() => _storedScoresAreArray.ShouldBeFalse();

    [Fact] void should_round_trip_the_pii_list_to_the_original_content() => _releasedScores.Contains(Criterion).ShouldBeTrue();

    AppendedEvent Event() =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject WorkSurface(string candidateName)
    {
        var score = Expando(("criterion", Criterion), ("score", Score));
        var candidate = Expando(("candidateId", "cand-1"), ("name", candidateName), ("evaluationScores", new object[] { score }));
        return Expando(("id", "req-1"), ("candidates", new object[] { candidate }));
    }
}
