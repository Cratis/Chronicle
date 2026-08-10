// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_the_key_cannot_be_converted.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// A root join whose event source id is not a Guid, against a Guid-backed column. There is no document it
/// can correctly reach, so the write must simply do nothing — and above all must not throw, because a sink
/// write that throws fails the partition and stops every later event for that event source.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_key_cannot_be_converted(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        protected override object? JoinKey => "not-a-guid";
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_leave_the_row_carrying_the_matching_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
}
