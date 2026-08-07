// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_there_is_no_key.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// A root join that resolved no key. An equality filter against null matches every document whose column is
/// null AND every document that does not carry the column at all, so an UpdateMany built from it stamps rows
/// that have nothing to do with the join. The write is refused instead.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_there_is_no_key(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        protected override object? JoinKey => null;
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_carrying_the_matching_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
}
