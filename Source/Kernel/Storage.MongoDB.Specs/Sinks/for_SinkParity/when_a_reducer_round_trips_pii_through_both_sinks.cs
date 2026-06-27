// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

/// <summary>
/// Cross-sink parity for a read model carrying real PII — a child collection whose children hold a scalar
/// <c>[PII]</c> name and a coarse <c>[PII]</c> list. The same two reduces are driven through both sinks
/// with the real compliance machinery (encrypt at rest), and the released (decrypted) read models are
/// compared. This is the gap that let the child-collection PII bugs ship green: the suite previously used
/// passthrough compliance and never exercised the encrypt-at-rest round trip through both sinks.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_reducer_round_trips_pii_through_both_sinks(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
{
    protected override bool ReleaseBeforeComparing => true;

    protected override IReadOnlyList<Func<ExpandoObject>> States =>
    [
        () => Surface("Alice", 5),
        () => Surface("Alice Smith", 7)
    ];

    protected override JsonSchema CreateSchema() =>
        JsonSchema.FromJson(
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
                      "scores": {
                        "type": "array",
                        "compliance": [ { "metadataType": "PII", "details": "" } ],
                        "items": { "type": "object", "properties": { "value": { "type": "integer" } } }
                      }
                    }
                  }
                }
              }
            }
            """);

    protected override IReadModelsCompliance CreateCompliance() =>
        new ReadModelsCompliance(
            new JsonComplianceManager(
                new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                    new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption()))),
            new Cratis.Chronicle.Json.ExpandoObjectConverter(new TypeFormats()));

    [Fact] void should_round_trip_identically_across_sinks() => ParityReport.ShouldEqual(string.Empty);

    [Fact] void should_have_a_result_from_both_sinks() => (InMemoryResult is not null && MongoResult is not null).ShouldBeTrue();

    static ExpandoObject Surface(string name, int score)
    {
        var scores = new object[] { Expando(("value", score)) };
        var candidate = Expando(("candidateId", "c1"), ("name", name), ("scores", scores));
        return Expando(("id", "root-1"), ("candidates", new object[] { candidate }));
    }
}
