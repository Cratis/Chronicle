// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_child_level_join.given;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_child_level_join.and_the_identifier_is_a_guid.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_child_level_join;

/// <summary>
/// The child branch reaching a Guid-backed child identifier — the behavior that already worked, pinned so it
/// cannot regress now that both branches share one conversion. The join reaches every root holding the child
/// and nothing else: neither the sibling member in the same root nor the root that does not hold it.
/// </summary>
/// <param name="ctx">The context the specs assert against.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_identifier_is_a_guid(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_over_roots_with_members(fixture)
    {
        protected override object? JoinIdentifier => MemberId.ToString();
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_stamp_the_member_in_the_root_that_holds_it() => ctx.StampedOn(a_sink_over_roots_with_members.RootHoldingTheMember, ctx.MemberId).ShouldEqual(a_sink_over_roots_with_members.StampedValue);
    [Fact] void should_stamp_the_member_in_every_other_root_that_holds_it() => ctx.StampedOn(a_sink_over_roots_with_members.SecondRootHoldingTheMember, ctx.MemberId).ShouldEqual(a_sink_over_roots_with_members.StampedValue);
    [Fact] void should_leave_the_sibling_member_alone() => ctx.StampedOn(a_sink_over_roots_with_members.RootHoldingTheMember, ctx.OtherMemberId).ShouldEqual(string.Empty);
    [Fact] void should_leave_the_root_without_the_member_untouched() => ctx.IsUnchanged(a_sink_over_roots_with_members.RootWithoutTheMember).ShouldBeTrue();
}
