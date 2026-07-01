// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Regression for serving a read model whose property is a coarse <c>[PII]</c> list through the read-model
/// release facade — the exact path <c>MaterializedReadModels.GetInstances</c> (queries, live/observable
/// reads) and command-side passive reads use. The released instance must carry the list as a real
/// collection, not the blob ciphertext string, otherwise every query/passive read of the model breaks the
/// same way the reducer state load does.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_the_query_path_releases_a_pii_list(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    const string Criterion = "quality";
    const int Score = 5;

    bool _releasedScoresAreCollection;
    bool _releasedScoresAreString;
    int _releasedScoreCount;

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
        await pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), State())));

        // Release the stored (encrypted) instance exactly as the query/passive read path does.
        var stored = await Sink.FindOrDefault(Key);
        var released = await Compliance.Release(EventStore, EventStoreNamespace, ReadModel.GetSchemaForLatestGeneration(), stored!);

        var scores = ((IDictionary<string, object?>)released!)["scores"];
        _releasedScoresAreString = scores is string;
        _releasedScoresAreCollection = scores is IEnumerable and not string;
        _releasedScoreCount = scores is IEnumerable enumerable and not string ? enumerable.Cast<object>().Count() : 0;
    }

    [Fact] void should_release_the_pii_list_as_a_collection() => _releasedScoresAreCollection.ShouldBeTrue();

    [Fact] void should_not_serve_the_pii_list_as_a_ciphertext_string() => _releasedScoresAreString.ShouldBeFalse();

    [Fact] void should_serve_the_single_element() => _releasedScoreCount.ShouldEqual(1);

    AppendedEvent Event() =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject State()
    {
        var score = Expando(("criterion", Criterion), ("score", Score));
        return Expando(("id", "req-1"), ("scores", new object[] { score }));
    }
}
