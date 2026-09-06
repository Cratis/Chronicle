// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_no_rows_match_but_the_changeset_changes_only_the_root_subject.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// The join itself matches nothing, so the root-level subject change has no document it can legally target.
/// The only correct outcome is that every stored row stays byte-identical to the seeded state.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_no_rows_match_but_the_changeset_changes_only_the_root_subject(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        readonly Guid _nonMatchingValue = Guid.NewGuid();

        protected override object? JoinKey => _nonMatchingValue.ToString();

        protected override IReadOnlyCollection<PropertyDifference> RootPropertyDifferences =>
        [
            new(new PropertyPath(WellKnownProperties.Subject), null, BlindSubject)
        ];
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_leave_the_row_carrying_the_matching_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
}
