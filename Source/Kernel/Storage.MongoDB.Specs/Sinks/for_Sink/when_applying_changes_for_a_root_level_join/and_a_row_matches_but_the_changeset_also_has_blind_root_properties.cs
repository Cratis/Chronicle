// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;
using MongoDB.Bson;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_a_row_matches_but_the_changeset_also_has_blind_root_properties.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// The filtered root join should still stamp the matched row, but the blind root properties must not hitch a
/// ride on an arbitrary UpdateOne. The matched row should therefore differ only by the joined payload itself,
/// and every unrelated row should stay byte-identical.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_row_matches_but_the_changeset_also_has_blind_root_properties(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        protected override object? JoinKey => MatchingValue.ToString();

        protected override IReadOnlyCollection<PropertyDifference> RootPropertyDifferences =>
        [
            new(new PropertyPath(WellKnownProperties.Subject), null, BlindSubject),
            new(new PropertyPath(OrdinaryRootProperty), string.Empty, OrdinaryRootValue)
        ];
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_stamp_the_row_carrying_the_matching_value() => ctx.StampedOn(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldEqual(a_sink_over_several_rows.StampedValue);
    [Fact]
    void should_change_only_the_join_payload_on_the_matching_row()
    {
        var expected = ctx.DocumentsBeforeTheJoin[a_sink_over_several_rows.RowWithTheMatchingValue].DeepClone().AsBsonDocument;
        expected[a_sink_over_several_rows.StampedProperty] = a_sink_over_several_rows.StampedValue;
        expected[WellKnownProperties.LastHandledEventSequenceNumber] = new BsonInt64((long)EventSequenceNumber.First.Value);

        ctx.DocumentsAfterTheJoin[a_sink_over_several_rows.RowWithTheMatchingValue].ShouldEqual(expected);
    }

    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
}
