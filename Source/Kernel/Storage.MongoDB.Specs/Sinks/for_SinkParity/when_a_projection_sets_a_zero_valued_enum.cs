// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

/// <summary>
/// "The projection set this property to the zero member" and "the projection never set this property" are
/// different facts, and they must not both come to rest as an absent field. They did: an explicitly projected
/// zero-valued enum was written as no field at all, so a nullable enum property read back as
/// <see langword="null"/> - indistinguishable from never having been set, and a deliberate choice silently
/// became a different, meaningful state that business logic then branched on.
/// </summary>
/// <remarks>
/// This cannot be settled by reading the sink back: the schema round-trip answers an absent field and a stored
/// zero alike, which is exactly why the defect survived. The assertion is on the raw stored BSON.
/// <para>
/// Its inverse - an unset property stored as a literal zero by a replay - is what makes the numbering question
/// unanswerable if only one of the two is fixed: a consumer told to 1-base an enum to survive this defect has no
/// member left to absorb the zero the other one writes.
/// </para>
/// </remarks>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_sets_a_zero_valued_enum(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
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
        () => Expando(("id", "root-1"), ("channel", 0))
    ];

    [Fact] void should_store_the_property_that_was_projected() => StoredDocument!.Contains("channel").ShouldBeTrue();
    [Fact] void should_store_the_zero_member_that_was_projected() => StoredDocument!["channel"].AsInt32.ShouldEqual(0);
    [Fact] void should_apply_identically_across_sinks() => ParityReport.ShouldEqual(string.Empty);
}
