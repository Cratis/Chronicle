// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.given;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.and_joined_and_parent_state_has_been_cached.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

/// <summary>
/// Revision after a completed replay uses revised joined and child content across repeated repairs.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(ChronicleCollection.Name)]
public class and_joined_and_parent_state_has_been_cached(context context) : Given<context>(context)
{
    /// <summary>
    /// Warm projections followed by separate completed mutations without recreating the fixture.
    /// </summary>
    /// <param name="fixture">The Chronicle fixture.</param>
    public class context(ChronicleFixture fixture) : joined_and_parent_state(fixture)
    {
        async Task Because()
        {
            await this.ReviseEvent(ReferenceSequenceNumber, new CrossSourceReferenceChanged(12));
            await CaptureJoinedAmount(12);
            await this.ReviseEvent(ReferenceSequenceNumber, new CrossSourceReferenceChanged(15));
            await CaptureJoinedAmount(15);
            await this.ReviseEvent(MutatedSequenceNumber, new ContentContributionRecorded(SharedDocument, 5));
            await CaptureAfterMutation(35, 5, 15);
        }
    }

    [Fact] void should_use_the_latest_value_in_the_first_document() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.FirstJoinedDocument).Amount.ShouldEqual(15);
    [Fact] void should_use_the_latest_value_in_the_second_document() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.SecondJoinedDocument).Amount.ShouldEqual(15);
    [Fact] void should_preserve_the_unrelated_joined_value() => Context.JoinedDocuments.Single(model => model.Id == joined_and_parent_state.UnrelatedJoinedDocument).Amount.ShouldEqual(7);
    [Fact] void should_rebuild_from_both_handled_join_types() => Context.JoinedDocuments.Select(model => model.Id).Order().ShouldEqual(new[] { joined_and_parent_state.FirstJoinedDocument, joined_and_parent_state.SecondJoinedDocument, joined_and_parent_state.UnrelatedJoinedDocument });
    [Fact] void should_replace_the_cached_child_content() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.SharedDocument).Items.Select(child => (child.Id, child.Amount)).OrderBy(child => child.Id).ShouldEqual(new[] { (content_mutation_projections.FirstSource, 5), (content_mutation_projections.SecondSource, 20) });
    [Fact] void should_preserve_the_unrelated_parent() => Context.ChildrenDocuments.Single(model => model.Id == content_mutation_projections.NeighborDocument).Items.Select(child => (child.Id, child.Amount)).ShouldEqual(new[] { (content_mutation_projections.NeighborSource, 7) });
}
