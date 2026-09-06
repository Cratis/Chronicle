// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_no_rows_match_but_the_changeset_changes_blind_root_properties_inside_a_bulk_window.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

/// <summary>
/// Bulk mode shares the same follow-up UpdateOne path as the immediate write path, so the blind root update
/// must be suppressed there too. A valid no-match root join with direct root properties must leave every
/// seeded row byte-identical even when the sink is inside a bulk window.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_no_rows_match_but_the_changeset_changes_blind_root_properties_inside_a_bulk_window(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_several_rows(fixture)
    {
        readonly Guid _nonMatchingValue = Guid.NewGuid();

        protected override object? JoinKey => _nonMatchingValue.ToString();

        protected override bool UseBulkMode => true;

        protected override IReadOnlyCollection<PropertyDifference> RootPropertyDifferences =>
        [
            new(new PropertyPath(WellKnownProperties.Subject), null, BlindSubject),
            new(new PropertyPath(OrdinaryRootProperty), string.Empty, OrdinaryRootValue)
        ];
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_leave_the_row_carrying_the_matching_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithTheMatchingValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_carrying_another_value_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithAnotherValue).ShouldBeTrue();
    [Fact] void should_leave_the_row_with_a_null_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithANullColumn).ShouldBeTrue();
    [Fact] void should_leave_the_row_without_the_column_untouched() => ctx.IsUnchanged(a_sink_over_several_rows.RowWithoutTheColumn).ShouldBeTrue();
}
