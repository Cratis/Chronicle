// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_the_key_matches_exactly_one_row.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// The join writes through UpdateMany, so it is not enough that the row it should reach is stamped — every
/// row it should not reach has to come out of the write byte-identical. This is the control the two refusal
/// specs beside it are measured against.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_key_matches_exactly_one_row(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        protected override object? JoinKey => MatchingValue.ToString();
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_stamp_the_row_carrying_the_value() => ctx.StampedOn(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldEqual(a_sink_over_several_rows.StampedValue);
    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
}
