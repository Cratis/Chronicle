// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

/// <summary>
/// The shape the defect was reported in: the read model exists first without the property, and a later event
/// sets it to the enum's zero member. That transition is the one a diff can lose - "was not there" to "is zero"
/// looks like no change to anything comparing against a default rather than against absence - and losing it
/// turns a deliberate choice into the state that means nobody has chosen yet.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_later_event_sets_a_zero_valued_enum(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
{
    protected override JsonSchema CreateSchema() => JsonSchema.FromJson(
        """
        {
            "type": "object",
            "properties": {
                "id": { "type": "string" },
                "channel": {
                    "type": ["integer", "null"],
                    "enum": [0, 1],
                    "x-enumNames": ["Email", "Conversation"]
                }
            }
        }
        """);

    protected override IReadOnlyList<Func<ExpandoObject>> States =>
    [
        () => Expando(("id", "root-1")),
        () => Expando(("id", "root-1"), ("channel", 0))
    ];

    [Fact] void should_store_the_property_the_later_event_set() => StoredDocument!.Contains("channel").ShouldBeTrue();
    [Fact] void should_store_the_zero_member_that_was_projected() => StoredDocument!["channel"].AsInt32.ShouldEqual(0);
    [Fact] void should_apply_identically_across_sinks() => ParityReport.ShouldEqual(string.Empty);
}
